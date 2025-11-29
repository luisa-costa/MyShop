using MyShop.Domain.Exceptions;

namespace MyShop.Domain;

/// <summary>
/// Entidade de domínio que representa um pedido.
/// Agregado raiz que gerencia seus próprios itens e regras de negócio.
/// </summary>
public class Order
{
    private readonly List<OrderItem> _items = new();

    public int Id { get; private set; }
    public string CustomerEmail { get; private set; }
    public Address ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Subtotal { get; private set; }
    public Money ShippingCost { get; private set; }
    public Money Discount { get; private set; }
    public Money Total => Subtotal + ShippingCost - Discount;

    // Propriedade de navegação para os itens do pedido
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Construtor privado para EF Core
    private Order() { }

    public Order(string customerEmail, Address shippingAddress)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new ArgumentException("Customer email cannot be empty", nameof(customerEmail));
        if (shippingAddress == null)
            throw new ArgumentNullException(nameof(shippingAddress));

        CustomerEmail = customerEmail;
        ShippingAddress = shippingAddress;
        CreatedAt = DateTime.UtcNow;
        Status = OrderStatus.Pending;
        Subtotal = new Money(0);
        ShippingCost = new Money(0);
        Discount = new Money(0);
    }

    /// <summary>
    /// Adiciona um item ao pedido.
    /// Método de domínio que encapsula a lógica de negócio.
    /// </summary>
    public void AddItem(OrderItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (Status != OrderStatus.Pending)
            throw new DomainException("Cannot add items to an order that is not pending");

        _items.Add(item);
        RecalculateSubtotal();
    }

    /// <summary>
    /// Remove um item do pedido.
    /// </summary>
    public void RemoveItem(Guid itemId)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Cannot remove items from an order that is not pending");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateSubtotal();
        }
    }

    /// <summary>
    /// Aplica um desconto ao pedido.
    /// </summary>
    public void ApplyDiscount(Money discountAmount)
    {
        if (discountAmount == null)
            throw new ArgumentNullException(nameof(discountAmount));
        if (discountAmount.Amount < 0)
            throw new ArgumentException("Discount cannot be negative", nameof(discountAmount));
        if (discountAmount.Amount > Subtotal.Amount)
            throw new DomainException("Discount cannot be greater than subtotal");

        Discount = discountAmount;
    }

    /// <summary>
    /// Define o custo de frete.
    /// </summary>
    public void SetShippingCost(Money shippingCost)
    {
        if (shippingCost == null)
            throw new ArgumentNullException(nameof(shippingCost));
        if (shippingCost.Amount < 0)
            throw new ArgumentException("Shipping cost cannot be negative", nameof(shippingCost));

        ShippingCost = shippingCost;
    }

    /// <summary>
    /// Confirma o pedido.
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Only pending orders can be confirmed");
        if (_items.Count == 0)
            throw new DomainException("Cannot confirm an order without items");

        Status = OrderStatus.Confirmed;
    }

    /// <summary>
    /// Cancela o pedido.
    /// </summary>
    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled");
        if (Status == OrderStatus.Shipped)
            throw new DomainException("Cannot cancel a shipped order");

        Status = OrderStatus.Cancelled;
    }

    /// <summary>
    /// Marca o pedido como enviado.
    /// </summary>
    public void MarkAsShipped()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException("Only confirmed orders can be shipped");

        Status = OrderStatus.Shipped;
    }

    /// <summary>
    /// Recalcula o subtotal baseado nos itens.
    /// </summary>
    private void RecalculateSubtotal()
    {
        Subtotal = _items.Aggregate(
            new Money(0),
            (sum, item) => sum + item.Subtotal
        );
    }
}

/// <summary>
/// Enum que representa o status de um pedido.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Cancelled = 3
}

