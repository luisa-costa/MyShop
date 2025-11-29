using Microsoft.EntityFrameworkCore;
using MyShop.Application.Interfaces;
using MyShop.Domain;
using MyShop.Infrastructure.Data;

namespace MyShop.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de pedidos usando Entity Framework Core.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly MyShopDbContext _context;

    public OrderRepository(MyShopDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

