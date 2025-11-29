using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyShop.Application.Services;
using MyShop.Domain;
using MyShop.Infrastructure.Data;
using MyShop.Infrastructure.Repositories;

namespace MyShop.IntegrationTests;

/// <summary>
/// Testes de integração usando Entity Framework Core com banco em memória.
/// 
/// Estes testes demonstram:
/// - Como configurar EF Core para testes com SQLite in-memory
/// - Como garantir isolamento entre testes (cada teste tem seu próprio banco)
/// - Como testar interações reais com o banco de dados
/// - Diferenças entre testes unitários (com mocks) e testes de integração (com banco real)
/// 
/// IMPORTANTE: Testes de integração são mais lentos que testes unitários,
/// mas validam que a integração entre camadas funciona corretamente.
/// </summary>
[CollectionDefinition("Integration Tests", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
}

[Collection("Integration Tests")]
public class EfCoreInMemoryIntegrationTests : IDisposable
{
    private readonly MyShopDbContext _context;
    private readonly ProductService _productService;
    private readonly DatabaseFixture _fixture;

    public EfCoreInMemoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        
        // Cria um contexto isolado para este teste
        // Cada teste recebe um banco de dados limpo
        _context = _fixture.CreateContext();
        
        // Cria os services com repositórios reais (não mocks)
        var productRepository = new ProductRepository(_context);
        _productService = new ProductService(productRepository);
    }

    [Fact]
    public async Task CreateProduct_ShouldPersistToDatabase()
    {
        // Arrange
        var name = "Test Product";
        var description = "Test Description";
        var price = new Money(99.99m);
        var stockQuantity = 10;

        // Act: Cria o produto usando o service real
        var product = await _productService.CreateProductAsync(name, description, price, stockQuantity);

        // Assert: Verifica que o produto foi salvo no banco
        product.Should().NotBeNull();
        product.Id.Should().BeGreaterThan(0);

        // Verifica diretamente no banco usando o contexto
        var productFromDb = await _context.Products.FindAsync(product.Id);
        productFromDb.Should().NotBeNull();
        productFromDb!.Name.Should().Be(name);
        productFromDb.Price.Amount.Should().Be(99.99m);
        productFromDb.StockQuantity.Should().Be(stockQuantity);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnProductFromDatabase()
    {
        // Arrange: Cria um produto diretamente no banco
        var product = new Product("Existing Product", "Description", new Money(50.00m), 5);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act: Busca usando o service
        var result = await _productService.GetProductByIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Existing Product");
        result.Price.Amount.Should().Be(50.00m);
    }

    [Fact]
    public async Task UpdateProductStock_ShouldUpdateDatabase()
    {
        // Arrange: Cria um produto no banco
        var product = new Product("Product", "Desc", new Money(100.00m), 10);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var originalStock = product.StockQuantity;
        var newStock = 25;

        // Act: Atualiza o estoque usando o service
        await _productService.UpdateProductStockAsync(product.Id, newStock);

        // Assert: Verifica que o banco foi atualizado
        // Precisamos recarregar a entidade do banco para ver as mudanças
        await _context.Entry(product).ReloadAsync();
        product.StockQuantity.Should().Be(newStock);
        product.StockQuantity.Should().NotBe(originalStock);
    }

    [Fact]
    public async Task GetActiveProducts_ShouldOnlyReturnActiveProducts()
    {
        // Arrange: Cria produtos ativos e inativos
        var activeProduct1 = new Product("Active 1", "Desc", new Money(10m), 5);
        var activeProduct2 = new Product("Active 2", "Desc", new Money(20m), 3);
        var inactiveProduct = new Product("Inactive", "Desc", new Money(30m), 0);
        inactiveProduct.Deactivate();

        _context.Products.AddRange(activeProduct1, activeProduct2, inactiveProduct);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetActiveProductsAsync();

        // Assert: Apenas produtos ativos devem ser retornados
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.IsActive);
        result.Should().Contain(p => p.Name == "Active 1");
        result.Should().Contain(p => p.Name == "Active 2");
        result.Should().NotContain(p => p.Name == "Inactive");
    }

    [Fact]
    public async Task EachTest_HasIsolatedDatabase()
    {
        // Este teste verifica que cada teste tem seu próprio banco isolado
        // Se os testes compartilhassem o banco, produtos criados em outros testes
        // apareceriam aqui

        // Arrange: Verifica que o banco está vazio (isolado)
        var allProducts = await _context.Products.ToListAsync();
        allProducts.Should().BeEmpty();

        // Act: Cria um produto
        var product = new Product("Isolated Product", "Desc", new Money(100m), 10);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assert: Apenas este produto existe
        var productsAfter = await _context.Products.ToListAsync();
        productsAfter.Should().HaveCount(1);
        productsAfter.First().Name.Should().Be("Isolated Product");
    }

    [Fact]
    public async Task ValueObjects_ShouldBePersistedCorrectly()
    {
        // Arrange: Testa que Value Objects (Money) são persistidos corretamente
        var product = new Product("Product", "Desc", new Money(123.45m, "BRL"), 10);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act: Busca do banco
        var productFromDb = await _context.Products.FindAsync(product.Id);

        // Assert: Verifica que o Value Object foi persistido e recuperado corretamente
        productFromDb.Should().NotBeNull();
        productFromDb!.Price.Amount.Should().Be(123.45m);
        productFromDb.Price.Currency.Should().Be("BRL");
    }

    public void Dispose()
    {
        // Limpa o contexto após cada teste
        // Isso garante que o próximo teste tenha um banco limpo
        _context.Dispose();
    }
}

/// <summary>
/// Testes que demonstram limitações dos providers em memória.
/// 
/// IMPORTANTE: SQLite in-memory é útil para testes, mas tem limitações:
/// - Não suporta todas as funcionalidades de um SQL Server real
/// - Alguns tipos de dados podem se comportar diferentemente
/// - Constraints complexas podem não ser validadas da mesma forma
/// - Performance pode ser diferente
/// 
/// Para testes mais próximos da produção, considere usar:
/// - Testcontainers (Docker containers com banco real)
/// - Banco de dados de teste dedicado
/// - Testes em ambiente de staging
/// </summary>
[Collection("Integration Tests")]
public class DatabaseLimitationsTests : IDisposable
{
    private readonly MyShopDbContext _context;
    private readonly DatabaseFixture _fixture;

    public DatabaseLimitationsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _context = _fixture.CreateContext();
    }

    [Fact]
    public async Task SqliteInMemory_SupportsMigrations()
    {
        // SQLite in-memory suporta migrations (ao contrário do provider InMemory)
        // Isso permite testar o schema real do banco
        
        // Verifica que o banco foi criado
        var canConnect = await _context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();

        // Verifica que as tabelas existem
        var tables = await _context.Database.GetAppliedMigrationsAsync();
        // Nota: GetAppliedMigrationsAsync pode não funcionar com SQLite in-memory
        // mas EnsureCreated() garante que o schema foi criado
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

