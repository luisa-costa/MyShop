using MyShop.Domain;

namespace MyShop.Application.Dtos;

/// <summary>
/// DTO que representa o resultado de uma operação de pedido.
/// </summary>
public class OrderResultDto
{
    public int OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public Money Total { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResultDto> Items { get; set; } = new();
}

/// <summary>
/// DTO para itens do pedido no resultado.
/// </summary>
public class OrderItemResultDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Money UnitPrice { get; set; } = null!;
    public Money Subtotal { get; set; } = null!;
}

