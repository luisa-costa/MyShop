namespace MyShop.Domain;

/// <summary>
/// Entidade que representa um item de um pedido.
/// Contém informações sobre o produto, quantidade e preço no momento da compra.
/// </summary>
public class OrderItem
{
    public Guid Id { get; private set; }
    public int OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money Subtotal => UnitPrice * Quantity;

    // Construtor privado para EF Core
    private OrderItem() { }

    public OrderItem(Guid productId, string productName, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be valid", nameof(productId));
        
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("ProductName cannot be empty", nameof(productName));
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        
        if (unitPrice == null)
            throw new ArgumentNullException(nameof(unitPrice));

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

