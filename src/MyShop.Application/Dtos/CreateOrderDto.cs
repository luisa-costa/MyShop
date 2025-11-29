using MyShop.Domain;

namespace MyShop.Application.Dtos;

/// <summary>
/// DTO (Data Transfer Object) para criação de pedidos.
/// DTOs são objetos simples usados para transferir dados entre camadas,
/// sem lógica de negócio.
/// </summary>
public class CreateOrderDto
{
    public string CustomerEmail { get; set; } = string.Empty;
    public Address ShippingAddress { get; set; } = null!;
    public List<OrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO para itens do pedido.
/// </summary>
public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

