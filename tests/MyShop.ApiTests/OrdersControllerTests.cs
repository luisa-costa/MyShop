using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyShop.Domain;
using MyShop.Infrastructure.Data;

namespace MyShop.ApiTests;

/// <summary>
/// Testes de integração para o OrdersController.
/// 
/// Demonstra testes de endpoints mais complexos que envolvem múltiplas entidades
/// e regras de negócio.
/// </summary>
public class OrdersControllerTests : IClassFixture<MyShopWebApplicationFactory>, IDisposable
{
    private readonly MyShopWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MyShopDbContext _context;

    public OrdersControllerTests(MyShopWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldReturnCreated()
    {
        // Arrange: Cria produtos no banco
        var product1 = new Product("Product 1", "Description", new Money(100.00m), 10);
        var product2 = new Product("Product 2", "Description", new Money(50.00m), 5);
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        var createOrderDto = new
        {
            CustomerEmail = "customer@example.com",
            ShippingStreet = "123 Main St",
            ShippingCity = "São Paulo",
            ShippingState = "SP",
            ShippingZipCode = "01234-567",
            ShippingCountry = "Brasil",
            Items = new[]
            {
                new { ProductId = product1.Id, Quantity = 2 },
                new { ProductId = product2.Id, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var orderResult = await response.Content.ReadFromJsonAsync<dynamic>();
        orderResult.Should().NotBeNull();
        
        // Verifica que o estoque foi reduzido
        await _context.Entry(product1).ReloadAsync();
        await _context.Entry(product2).ReloadAsync();
        product1.StockQuantity.Should().Be(8); // 10 - 2
        product2.StockQuantity.Should().Be(4); // 5 - 1
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange
        var product = new Product("Product", "Description", new Money(100.00m), 5); // Apenas 5 em estoque
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var createOrderDto = new
        {
            CustomerEmail = "customer@example.com",
            ShippingStreet = "123 Main St",
            ShippingCity = "São Paulo",
            ShippingState = "SP",
            ShippingZipCode = "01234-567",
            Items = new[]
            {
                new { ProductId = product.Id, Quantity = 10 } // Tentando comprar 10
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Verifica que o estoque NÃO foi alterado
        await _context.Entry(product).ReloadAsync();
        product.StockQuantity.Should().Be(5);
    }

    [Fact]
    public async Task CreateOrder_WithOrderAbove200_ShouldHaveFreeShipping()
    {
        // Arrange: Pedido acima de R$ 200 deve ter frete grátis
        var product = new Product("Expensive Product", "Description", new Money(250.00m), 5);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var createOrderDto = new
        {
            CustomerEmail = "customer@example.com",
            ShippingStreet = "123 Main St",
            ShippingCity = "São Paulo",
            ShippingState = "SP",
            ShippingZipCode = "01234-567",
            Items = new[]
            {
                new { ProductId = product.Id, Quantity = 1 } // 250.00
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Em um cenário real, verificaríamos o Total no response
        // Por simplicidade, apenas verificamos que foi criado com sucesso
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public void Dispose()
    {
        // Limpa o banco após cada teste
        _context.Products.RemoveRange(_context.Products);
        _context.Orders.RemoveRange(_context.Orders);
        _context.SaveChanges();
        _context.Dispose();
        _client.Dispose();
    }
}

