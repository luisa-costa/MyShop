using MyShop.Domain;

namespace MyShop.Application.Interfaces;

/// <summary>
/// Interface do repositório de pedidos.
/// Define o contrato para acesso a dados de pedidos.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

