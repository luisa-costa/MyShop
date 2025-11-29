using Microsoft.EntityFrameworkCore;
using MyShop.Domain;

namespace MyShop.Infrastructure.Data;

/// <summary>
/// DbContext do Entity Framework Core para o MyShop.
/// Representa a sessão com o banco de dados e permite consultar e salvar entidades.
/// </summary>
public class MyShopDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public MyShopDbContext(DbContextOptions<MyShopDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da entidade Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();

            // Configuração do Value Object Money como propriedades escalares
            entity.OwnsOne(e => e.Price, price =>
            {
                price.Property(p => p.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)").IsRequired();
                price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(10).IsRequired();
            });
        });

        // Configuração da entidade Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<int>();

            // Configuração do Value Object Address
            entity.OwnsOne(e => e.ShippingAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200).IsRequired();
                address.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100).IsRequired();
                address.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(50).IsRequired();
                address.Property(a => a.ZipCode).HasColumnName("ShippingZipCode").HasMaxLength(20).IsRequired();
                address.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100).IsRequired();
            });

            // Configuração dos Value Objects Money
            entity.OwnsOne(e => e.Subtotal, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Subtotal").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("SubtotalCurrency").HasMaxLength(10).IsRequired();
            });

            entity.OwnsOne(e => e.ShippingCost, money =>
            {
                money.Property(m => m.Amount).HasColumnName("ShippingCost").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("ShippingCostCurrency").HasMaxLength(10).IsRequired();
            });

            entity.OwnsOne(e => e.Discount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Discount").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("DiscountCurrency").HasMaxLength(10).IsRequired();
            });

            // Relacionamento com OrderItems
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey("OrderId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuração da entidade OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).IsRequired();

            // Configuração do Value Object Money
            entity.OwnsOne(e => e.UnitPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(10).IsRequired();
            });
        });
    }
}

