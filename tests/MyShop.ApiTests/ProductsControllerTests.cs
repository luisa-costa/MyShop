using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.API.Controllers;
using MyShop.Application.Dtos;
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        Assert.NotNull(products);
        Assert.True(products!.Count > 0);
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var returnedProduct = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(returnedProduct);
        Assert.Equal("Test Product", returnedProduct!.Name);
        Assert.Equal(99.99m, returnedProduct.Price.Amount);
    }

    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange: Usa um Guid que não existe no banco
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/products/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync(); 
        var createdProduct = JsonSerializer.Deserialize<CreatedProductDto>(responseContent);

        // Verifica que o produto foi salvo no banco fazendo uma requisição GET
        // Isso garante que o produto está acessível através da API
        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var productFromApi = await getResponse.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(productFromApi);
        Assert.Equal("New Product", productFromApi.Name);
        Assert.Equal(150.00m, productFromApi.Price.Amount);
        Assert.Equal(20, productFromApi.StockQuantity);
        
        // Verifica também diretamente no banco usando um novo escopo
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        
        var productFromDb = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == createdProduct.Id);
        
        Assert.NotNull(productFromDb);
        Assert.Equal(createdProduct.Id, productFromDb!.Id);
        Assert.Equal("New Product", productFromDb.Name);
        Assert.Equal(150.00m, productFromDb.Price.Amount);
        Assert.Equal(20, productFromDb.StockQuantity);
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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verifica que o estoque foi atualizado no banco
        await _context.Entry(product).ReloadAsync();
        Assert.Equal(25, product.StockQuantity);
    }

    [Fact]
    public async Task UpdateStock_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange: Usa um Guid que não existe no banco
        var nonExistentId = Guid.NewGuid();
        var updateStockDto = new
        {
            StockQuantity = 25
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{nonExistentId}/stock", updateStockDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        // Limpa o banco após cada teste
        // Como estamos usando SQLite in-memory, o banco será descartado automaticamente
        // Mas limpamos explicitamente para garantir isolamento entre testes
        _context.Products.RemoveRange(_context.Products);
        _context.SaveChanges();
        _context.Dispose();
        _client?.Dispose();
    }
}

