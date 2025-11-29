namespace MyShop.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando não há estoque suficiente para um produto.
/// Exemplo de exceção específica de domínio que representa uma regra de negócio.
/// </summary>
public class InsufficientStockException : DomainException
{
    public string ProductName { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(string productName, int requestedQuantity, int availableQuantity)
        : base($"Insufficient stock for product '{productName}'. Requested: {requestedQuantity}, Available: {availableQuantity}")
    {
        ProductName = productName;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}

