using MyShop.Application.Dtos;
using MyShop.Application.Interfaces;
using MyShop.Application.Services;
using MyShop.Domain;
using MyShop.Domain.Exceptions;
using Moq;

namespace MyShop.UnitTests.Services;

/// <summary>
/// Testes unitários para OrderService focando na lógica de negócio.
/// 
/// Estes testes demonstram:
/// - Testes de regras de negócio complexas (cálculo de frete, descontos)
/// - Validações de estoque
/// - Casos de sucesso e falha
/// 
/// NOTA: Estes testes usam mocks, mas focam na lógica de negócio do service.
/// Para testes com mocks mais detalhados, veja OrderServiceWithMocksTests.
/// </summary>
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IPaymentGateway> _mockPaymentGateway;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockPaymentGateway = new Mock<IPaymentGateway>();
        _mockEmailSender = new Mock<IEmailSender>();

        _service = new OrderService(
            _mockOrderRepository.Object,
            _mockProductRepository.Object,
            _mockPaymentGateway.Object,
            _mockEmailSender.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidData_ShouldCreateOrder()
    {
        // Arrange
        var product = new Product("Product 1", "Description", new Money(100.00m), 10);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 2 }
            }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        Order? savedOrder = null;
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) =>
            {
                savedOrder = o;
                return o;
            });

        _mockPaymentGateway
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TXN-123");

        // Act
        var result = await _service.CreateOrderAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.CustomerEmail, result.CustomerEmail);
        Assert.Equal(OrderStatus.Confirmed, result.Status);
        Assert.Single(result.Items);
        
        // Verifica que o estoque foi reduzido
        Assert.Equal(8, product.StockQuantity); // 10 - 2 = 8
    }

    [Fact]
    public async Task CreateOrderAsync_WithOrderAbove200_ShouldHaveFreeShipping()
    {
        // Arrange: Pedido acima de R$ 200 deve ter frete grátis
        var product = new Product("Expensive Product", "Desc", new Money(250.00m), 5);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 1 } // 250.00
            }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        Order? savedOrder = null;
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) =>
            {
                savedOrder = o;
                return o;
            });

        _mockPaymentGateway
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TXN-123");

        // Act
        var result = await _service.CreateOrderAsync(dto);

        // Assert: Frete deve ser zero
        Assert.Equal(0, savedOrder!.ShippingCost.Amount);
        Assert.Equal(250.00m, result.Total.Amount); // Sem frete
    }

    [Fact]
    public async Task CreateOrderAsync_WithOrderBelow200_ShouldHaveShippingCost()
    {
        // Arrange: Pedido abaixo de R$ 200 deve ter frete de R$ 15
        var product = new Product("Cheap Product", "Desc", new Money(50.00m), 10);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 1 } // 50.00
            }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        Order? savedOrder = null;
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) =>
            {
                savedOrder = o;
                return o;
            });

        _mockPaymentGateway
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TXN-123");

        // Act
        var result = await _service.CreateOrderAsync(dto);

        // Assert: Frete deve ser R$ 15
        Assert.Equal(15.00m, savedOrder!.ShippingCost.Amount);
        Assert.Equal(65.00m, result.Total.Amount); // 50 + 15
    }

    [Fact]
    public async Task CreateOrderAsync_WithOrderAbove500_ShouldHaveDiscount()
    {
        // Arrange: Pedido acima de R$ 500 deve ter 10% de desconto
        var product = new Product("Very Expensive Product", "Desc", new Money(600.00m), 2);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 1 } // 600.00
            }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        Order? savedOrder = null;
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) =>
            {
                savedOrder = o;
                return o;
            });

        _mockPaymentGateway
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TXN-123");

        // Act
        var result = await _service.CreateOrderAsync(dto);

        // Assert: Deve ter desconto de 10% (60.00) e frete grátis
        Assert.Equal(60.00m, savedOrder!.Discount.Amount);
        Assert.Equal(0, savedOrder.ShippingCost.Amount); // Frete grátis acima de 200
        Assert.Equal(540.00m, result.Total.Amount); // 600 - 60
    }

    [Fact]
    public async Task CreateOrderAsync_WithInsufficientStock_ShouldThrowInsufficientStockException()
    {
        // Arrange
        var product = new Product("Product", "Desc", new Money(100.00m), 5); // Apenas 5 em estoque
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto>
            {
                new() { ProductId = product.Id, Quantity = 10 } // Tentando comprar 10
            }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(async () =>
            await _service.CreateOrderAsync(dto));
        
        // Verifica que o pedido NÃO foi salvo
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_WithEmptyItems_ShouldThrowDomainException()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto>() // Lista vazia
        };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(async () =>
            await _service.CreateOrderAsync(dto));
    }

    [Fact]
    public async Task CancelOrderAsync_WhenOrderExists_ShouldCancelAndRestoreStock()
    {
        // Arrange
        var product = new Product("Product", "Desc", new Money(100.00m), 5);
        var order = new Order("customer@example.com", new Address("Street", "City", "State", "12345"));
        order.AddItem(new OrderItem(product.Id, "Product", 2, new Money(100.00m)));
        order.Confirm();

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _service.CancelOrderAsync(1);

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(7, product.StockQuantity); // 5 + 2 = 7 (estoque restaurado)
        _mockOrderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _mockProductRepository.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }
}

