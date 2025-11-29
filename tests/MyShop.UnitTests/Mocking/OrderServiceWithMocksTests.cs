using FluentAssertions;
using MyShop.Application.Dtos;
using MyShop.Application.Interfaces;
using MyShop.Application.Services;
using MyShop.Domain;
using Moq;

namespace MyShop.UnitTests.Mocking;

/// <summary>
/// Testes que demonstram o uso avançado de Moq para mockar dependências.
/// 
/// Estes testes focam em:
/// - Setup de métodos com diferentes cenários (Returns, Throws)
/// - Verificação de chamadas (Verify, VerifyNoOtherCalls)
/// - Uso de It.IsAny<T>() e It.Is<T>(predicate)
/// - Callbacks para capturar parâmetros
/// - Diferença entre testar lógica real vs dependências mockadas
/// </summary>
public class OrderServiceWithMocksTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IPaymentGateway> _mockPaymentGateway;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly OrderService _service;

    public OrderServiceWithMocksTests()
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
    public async Task CreateOrderAsync_ShouldCallPaymentGatewayWithCorrectAmount()
    {
        // Arrange
        var product = new Product("Product", "Desc", new Money(100.00m), 10);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
        await _service.CreateOrderAsync(dto);

        // Assert: Verifica que ProcessPaymentAsync foi chamado
        // It.IsAny<Money>() aceita qualquer valor Money
        _mockPaymentGateway.Verify(
            g => g.ProcessPaymentAsync(
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verifica que foi chamado com o valor total correto
        // It.Is<Money>(predicate) permite verificar condições específicas
        _mockPaymentGateway.Verify(
            g => g.ProcessPaymentAsync(
                It.Is<Money>(m => m.Amount == savedOrder!.Total.Amount),
                It.Is<string>(email => email == "customer@example.com"),
                It.Is<string>(desc => desc.Contains("Order #")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldSendEmailAfterPayment()
    {
        // Arrange
        var product = new Product("Product", "Desc", new Money(100.00m), 10);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        _mockPaymentGateway
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TXN-456");

        // Act
        await _service.CreateOrderAsync(dto);

        // Assert: Verifica que o email foi enviado
        _mockEmailSender.Verify(
            e => e.SendEmailAsync(
                "customer@example.com",
                It.Is<string>(subject => subject.Contains("Confirmed")),
                It.Is<string>(body => body.Contains("TXN-456")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldNotCallPaymentGatewayIfOrderCreationFails()
    {
        // Arrange: Simula uma falha ao salvar o pedido
        var product = new Product("Product", "Desc", new Money(100.00m), 10);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Simula uma exceção ao salvar
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _service.CreateOrderAsync(dto));

        // Verifica que o gateway de pagamento NÃO foi chamado
        _mockPaymentGateway.Verify(
            g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCallDependenciesInCorrectOrder()
    {
        // Arrange
        var product = new Product("Product", "Desc", new Money(100.00m), 10);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        var callOrder = new List<string>();

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product)
            .Callback(() => callOrder.Add("GetProduct"));

        _mockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => callOrder.Add("UpdateProduct"));

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o)
            .Callback(() => callOrder.Add("AddOrder"));

        _mockPaymentGateway
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TXN-123")
            .Callback(() => callOrder.Add("ProcessPayment"));

        _mockEmailSender
            .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => callOrder.Add("SendEmail"));

        // Act
        await _service.CreateOrderAsync(dto);

        // Assert: Verifica a ordem das chamadas
        callOrder.Should().ContainInOrder(
            "GetProduct",
            "UpdateProduct",
            "AddOrder",
            "ProcessPayment",
            "SendEmail");
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldVerifyNoOtherCalls()
    {
        // Arrange
        var product = new Product("Product", "Desc", new Money(100.00m), 10);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        _mockPaymentGateway
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TXN-123");

        // Act
        await _service.CreateOrderAsync(dto);

        // Assert: Verifica que não houve outras chamadas além das esperadas
        _mockPaymentGateway.VerifyNoOtherCalls();
        _mockEmailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelOrderAsync_ShouldRefundPayment()
    {
        // Arrange
        var order = new Order("customer@example.com", new Address("Street", "City", "State", "12345"));
        order.AddItem(new OrderItem(1, "Product", 1, new Money(100.00m)));
        order.Confirm();

        var product = new Product("Product", "Desc", new Money(100.00m), 5);

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _service.CancelOrderAsync(1);

        // Assert: Verifica que o email de cancelamento foi enviado
        _mockEmailSender.Verify(
            e => e.SendEmailAsync(
                "customer@example.com",
                It.Is<string>(subject => subject.Contains("Cancelled")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithPaymentGatewayThrowingException_ShouldPropagateException()
    {
        // Arrange
        var product = new Product("Product", "Desc", new Money(100.00m), 10);
        var dto = new CreateOrderDto
        {
            CustomerEmail = "customer@example.com",
            ShippingAddress = new Address("Street", "City", "State", "12345"),
            Items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        _mockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Simula uma falha no gateway de pagamento
        _mockPaymentGateway
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Payment gateway unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _service.CreateOrderAsync(dto));

        // Verifica que o email NÃO foi enviado (pois o pagamento falhou)
        _mockEmailSender.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

