using Orders.Application.Interfaces;
using Orders.Infrastructure.Database;
using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Orders.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrdersDbContext _dbContext;
        public OrderRepository(OrdersDbContext dbContext) => _dbContext = dbContext;
         
        public async Task<Order?> GetByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Orders
                .Include(o => o.Items) 
                .Include(o => o.StatusHistories)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Orders
                .Include(o => o.Items) 
                .Include(o => o.StatusHistories)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Orders
                .FromSqlRaw("SELECT * FROM \"orders\".\"Orders\" WHERE \"Id\" = {0} FOR UPDATE", id)
                .Include(o => o.Items)           // BẮT BUỘC PHẢI CÓ
                .Include(o => o.StatusHistories) // BẮT BUỘC PHẢI CÓ
                .FirstOrDefaultAsync(cancellationToken);
        }
        public void Add(Order order)
        {
            _dbContext.Orders.Add(order);
        }
    }
}
