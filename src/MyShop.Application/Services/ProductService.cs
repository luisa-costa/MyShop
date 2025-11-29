using MyShop.Application.Interfaces;
using MyShop.Domain;
using MyShop.Domain.Exceptions;

namespace MyShop.Application.Services;

/// <summary>
/// Service que contém a lógica de negócio relacionada a produtos.
/// Services orquestram operações de domínio e coordenam com repositórios.
/// </summary>
public class ProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    /// <summary>
    /// Obtém um produto por ID.
    /// </summary>
    public async Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _productRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Obtém todos os produtos ativos.
    /// </summary>
    public async Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        var allProducts = await _productRepository.GetAllAsync(cancellationToken);
        return allProducts.Where(p => p.IsActive);
    }

    /// <summary>
    /// Cria um novo produto.
    /// </summary>
    public async Task<Product> CreateProductAsync(string name, string description, Money price, int stockQuantity, CancellationToken cancellationToken = default)
    {
        // Validações de negócio
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");

        if (price.Amount <= 0)
            throw new DomainException("Product price must be greater than zero");

        if (stockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative");

        var product = new Product(name, description, price, stockQuantity);
        return await _productRepository.AddAsync(product, cancellationToken);
    }

    /// <summary>
    /// Atualiza o estoque de um produto.
    /// </summary>
    public async Task UpdateProductStockAsync(Guid productId, int newStockQuantity, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
            throw new DomainException($"Product with ID {productId} not found");

        if (newStockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative");

        // Calcula a diferença e ajusta o estoque
        var difference = newStockQuantity - product.StockQuantity;
        if (difference > 0)
            product.IncreaseStock(difference);
        else if (difference < 0)
            product.ReduceStock(-difference);

        await _productRepository.UpdateAsync(product, cancellationToken);
    }
}

