namespace MyShop.Domain;

/// <summary>
/// Entidade de domínio que representa um produto.
/// Entidades têm identidade única (Id) e podem mudar ao longo do tempo.
/// </summary>
public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    // Construtor privado para garantir que a criação seja feita através de métodos controlados
    private Product() { }

    public Product(string name, string description, Money price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));
        if (price == null)
            throw new ArgumentNullException(nameof(price));
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

        Name = name;
        Description = description ?? string.Empty;
        Price = price;
        StockQuantity = stockQuantity;
        IsActive = true;
    }

    /// <summary>
    /// Reduz o estoque do produto.
    /// Método de domínio que encapsula a lógica de negócio.
    /// </summary>
    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        
        if (StockQuantity < quantity)
            throw new Exceptions.InsufficientStockException(Name, quantity, StockQuantity);

        StockQuantity -= quantity;
    }

    /// <summary>
    /// Aumenta o estoque do produto.
    /// </summary>
    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        StockQuantity += quantity;
    }

    /// <summary>
    /// Verifica se há estoque suficiente.
    /// </summary>
    public bool HasStock(int quantity)
    {
        return StockQuantity >= quantity;
    }

    /// <summary>
    /// Desativa o produto.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Ativa o produto.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}

