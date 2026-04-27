using Orders.Application.Interfaces;
using Orders.Infrastructure.Database;

namespace Orders.Infrastructure.Repositories
{
    public class OrdersUnitOfWork : IOrdersUnitOfWork
    {
        private readonly OrdersDbContext _context;

        public OrdersUnitOfWork(OrdersDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
