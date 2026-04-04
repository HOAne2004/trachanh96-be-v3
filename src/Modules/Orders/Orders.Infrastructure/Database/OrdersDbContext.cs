using Microsoft.EntityFrameworkCore;
using Orders.Domain.Entities;
using Shared.Infrastructure.Outbox;
using Orders.Application.Interfaces;

namespace Orders.Infrastructure.Database
{
    public class OrdersDbContext : DbContext, IOrdersDbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
        {
        }
        // Các DbSet đại diện cho các bảng
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("orders");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
        }
    }
}
