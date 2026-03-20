using Shared.Application.Interfaces;

namespace Catalog.Infrastructure.Database
{
    public class CatalogUnitOfWork : IUnitOfWork
    {
        private readonly CatalogDbContext _context;
        public CatalogUnitOfWork(CatalogDbContext context) 
        {
            _context = context;
        }
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
