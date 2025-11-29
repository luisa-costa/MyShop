using MyShop.Domain;

namespace MyShop.Application.Interfaces;

/// <summary>
/// Interface do repositório de produtos.
/// Define o contrato para acesso a dados de produtos.
/// Usado para permitir inversão de dependência e facilitar testes.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

