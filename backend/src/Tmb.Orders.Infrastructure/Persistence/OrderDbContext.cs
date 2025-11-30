using Microsoft.EntityFrameworkCore;
using Tmb.Orders.Domain.Entities;

namespace Tmb.Orders.Infrastructure.Persistence;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);

            e.Property(o => o.Cliente)
                .IsRequired()
                .HasMaxLength(200);

            e.Property(o => o.Produto)
                .IsRequired()
                .HasMaxLength(200);

            e.Property(o => o.Valor)
                .HasColumnType("numeric(18,2)");

            e.Property(o => o.Status)
                .IsRequired();

            e.Property(o => o.DataCriacao)
                .IsRequired();

            e.Property(o => o.LastProcessedMessageId)
                .HasMaxLength(200);

            e.HasMany(o => o.StatusHistory)
                .WithOne()
                .HasForeignKey(h => h.OrderId);
        });

        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(h => h.Id);

            e.Property(h => h.Status)
                .IsRequired();

            e.Property(h => h.ChangedAt)
                .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
