using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyShop.Domain;
using MyShop.Infrastructure.Data;

namespace MyShop.ApiTests;

/// <summary>
/// Testes de integração para o ProductsController.
/// 
/// Estes testes demonstram:
/// - Como testar APIs usando WebApplicationFactory
/// - Como fazer requisições HTTP reais para os endpoints
/// - Como verificar respostas HTTP (status codes, body)
/// - Diferença entre testes automatizados em C# vs testes manuais no Postman
/// 
/// VANTAGENS dos testes automatizados em C#:
/// - Podem ser executados em CI/CD
/// - Mais rápidos que testes manuais
/// - Garantem que a API funciona após mudanças
/// - Podem ser executados em paralelo
/// 
/// VANTAGENS dos testes no Postman:
/// - Interface visual amigável
/// - Fácil de compartilhar com equipe não-técnica
/// - Permite testes exploratórios
/// - Útil para documentação e exemplos
/// </summary>
public class ProductsControllerTests : IClassFixture<MyShopWebApplicationFactory>, IDisposable
{
    private readonly MyShopWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MyShopDbContext _context;

    public ProductsControllerTests(MyShopWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Cria um escopo para acessar o DbContext
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
    }

    [Fact]
    public async Task GetProducts_ShouldReturnOk()
    {
        // Arrange: Adiciona alguns produtos ao banco
        _context.Products.AddRange(
            new Product("Product 1", "Description 1", new Money(10.00m), 5),
            new Product("Product 2", "Description 2", new Money(20.00m), 10)
        );
        await _context.SaveChangesAsync();

        // Act: Faz uma requisição GET para /api/products
        var response = await _client.GetAsync("/api/products");

        // Assert: Verifica o status code e o conteúdo
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().NotBeNull();
        products!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProduct_WhenProductExists_ShouldReturnProduct()
    {
        // Arrange
        var product = new Product("Test Product", "Test Description", new Money(99.99m), 10);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var returnedProduct = await response.Content.ReadFromJsonAsync<Product>();
        returnedProduct.Should().NotBeNull();
        returnedProduct!.Name.Should().Be("Test Product");
        returnedProduct.Price.Amount.Should().Be(99.99m);
    }

    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/products/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var createProductDto = new
        {
            Name = "New Product",
            Description = "New Description",
            Price = 150.00m,
            Currency = "BRL",
            StockQuantity = 20
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createProductDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdProduct = await response.Content.ReadFromJsonAsync<Product>();
        createdProduct.Should().NotBeNull();
        createdProduct!.Name.Should().Be("New Product");
        createdProduct.Price.Amount.Should().Be(150.00m);
        createdProduct.StockQuantity.Should().Be(20);

        // Verifica que o produto foi salvo no banco
        var productFromDb = await _context.Products.FindAsync(createdProduct.Id);
        productFromDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange: DTO com dados inválidos (nome vazio)
        var createProductDto = new
        {
            Name = "", // Nome vazio é inválido
            Description = "Description",
            Price = 100.00m,
            Currency = "BRL",
            StockQuantity = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createProductDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStock_WithValidData_ShouldReturnNoContent()
    {
        // Arrange
        var product = new Product("Product", "Description", new Money(100.00m), 10);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var updateStockDto = new
        {
            StockQuantity = 25
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{product.Id}/stock", updateStockDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verifica que o estoque foi atualizado no banco
        await _context.Entry(product).ReloadAsync();
        product.StockQuantity.Should().Be(25);
    }

    [Fact]
    public async Task UpdateStock_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var updateStockDto = new
        {
            StockQuantity = 25
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/products/99999/stock", updateStockDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        // Limpa o banco após cada teste
        _context.Products.RemoveRange(_context.Products);
        _context.SaveChanges();
        _context.Dispose();
        _client.Dispose();
    }
}

