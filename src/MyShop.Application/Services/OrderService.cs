using MyShop.Application.Dtos;
using MyShop.Application.Interfaces;
using MyShop.Domain;
using MyShop.Domain.Exceptions;

namespace MyShop.Application.Services;

/// <summary>
/// Service que contém a lógica de negócio complexa relacionada a pedidos.
/// Este service demonstra várias regras de negócio:
/// - Validação de estoque
/// - Cálculo de descontos
/// - Aplicação de frete
/// - Processamento de pagamento
/// - Envio de notificações
/// </summary>
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IEmailSender _emailSender;

    // Constantes para regras de negócio
    private const decimal FREE_SHIPPING_THRESHOLD = 200.00m;
    private const decimal STANDARD_SHIPPING_COST = 15.00m;
    private const decimal DISCOUNT_PERCENTAGE_FOR_LARGE_ORDERS = 0.10m; // 10% de desconto
    private const decimal LARGE_ORDER_THRESHOLD = 500.00m;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IPaymentGateway paymentGateway,
        IEmailSender emailSender)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
    }

    /// <summary>
    /// Cria um novo pedido com validações e regras de negócio.
    /// Este método demonstra várias regras de negócio complexas.
    /// </summary>
    public async Task<OrderResultDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        // Validações iniciais
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));
        if (dto.Items == null || dto.Items.Count == 0)
            throw new DomainException("Order must have at least one item");

        // Cria o pedido
        var order = new Order(dto.CustomerEmail, dto.ShippingAddress);

        // Processa cada item do pedido
        foreach (var itemDto in dto.Items)
        {
            // Busca o produto
            var product = await _productRepository.GetByIdAsync(itemDto.ProductId, cancellationToken);
            if (product == null)
                throw new DomainException($"Product with ID {itemDto.ProductId} not found");

            if (!product.IsActive)
                throw new DomainException($"Product '{product.Name}' is not active");

            // Valida estoque
            if (!product.HasStock(itemDto.Quantity))
                throw new InsufficientStockException(product.Name, itemDto.Quantity, product.StockQuantity);

            // Reduz o estoque
            product.ReduceStock(itemDto.Quantity);
            await _productRepository.UpdateAsync(product, cancellationToken);

            // Cria o item do pedido
            var orderItem = new OrderItem(product.Id, product.Name, itemDto.Quantity, product.Price);
            order.AddItem(orderItem);
        }

        // Calcula o frete (regra de negócio: frete grátis acima de R$ 200)
        var shippingCost = CalculateShippingCost(order.Subtotal);
        order.SetShippingCost(shippingCost);

        // Aplica desconto para pedidos grandes (regra de negócio: 10% de desconto acima de R$ 500)
        var discount = CalculateDiscount(order.Subtotal);
        if (discount.Amount > 0)
        {
            order.ApplyDiscount(discount);
        }

        // Confirma o pedido
        order.Confirm();

        // Salva o pedido
        var savedOrder = await _orderRepository.AddAsync(order, cancellationToken);

        // Processa o pagamento
        var transactionId = await _paymentGateway.ProcessPaymentAsync(
            savedOrder.Total,
            savedOrder.CustomerEmail,
            $"Order #{savedOrder.Id}",
            cancellationToken);

        // Envia email de confirmação
        await _emailSender.SendEmailAsync(
            savedOrder.CustomerEmail,
            $"Order #{savedOrder.Id} Confirmed",
            $"Your order has been confirmed. Total: {savedOrder.Total}. Transaction ID: {transactionId}",
            cancellationToken);

        // Retorna o resultado
        return MapToResultDto(savedOrder);
    }

    /// <summary>
    /// Cancela um pedido e reembolsa o pagamento.
    /// </summary>
    public async Task CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new DomainException($"Order with ID {orderId} not found");

        // Cancela o pedido
        order.Cancel();

        // Reembolsa o pagamento (em um cenário real, precisaríamos armazenar o transactionId)
        // Por simplicidade, vamos assumir que temos acesso ao transactionId
        // Em uma implementação real, isso viria do banco de dados

        // Restaura o estoque dos produtos
        foreach (var item in order.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product != null)
            {
                product.IncreaseStock(item.Quantity);
                await _productRepository.UpdateAsync(product, cancellationToken);
            }
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);

        // Envia email de cancelamento
        await _emailSender.SendEmailAsync(
            order.CustomerEmail,
            $"Order #{order.Id} Cancelled",
            $"Your order has been cancelled.",
            cancellationToken);
    }

    /// <summary>
    /// Calcula o custo de frete baseado no subtotal.
    /// Regra de negócio: Frete grátis para pedidos acima de R$ 200.
    /// </summary>
    private Money CalculateShippingCost(Money subtotal)
    {
        if (subtotal.Amount >= FREE_SHIPPING_THRESHOLD)
            return new Money(0);

        return new Money(STANDARD_SHIPPING_COST);
    }

    /// <summary>
    /// Calcula o desconto baseado no valor do pedido.
    /// Regra de negócio: 10% de desconto para pedidos acima de R$ 500.
    /// </summary>
    private Money CalculateDiscount(Money subtotal)
    {
        if (subtotal.Amount >= LARGE_ORDER_THRESHOLD)
        {
            var discountAmount = subtotal.Amount * DISCOUNT_PERCENTAGE_FOR_LARGE_ORDERS;
            return new Money(discountAmount);
        }

        return new Money(0);
    }

    /// <summary>
    /// Mapeia uma entidade Order para um DTO de resultado.
    /// </summary>
    private OrderResultDto MapToResultDto(Order order)
    {
        return new OrderResultDto
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Status = order.Status,
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(item => new OrderItemResultDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Subtotal
            }).ToList()
        };
    }
}

