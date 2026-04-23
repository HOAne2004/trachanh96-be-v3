using Catalog.Application.Interfaces;
using Catalog.Infrastructure.Database;

namespace Catalog.Infrastructure.Repositories
{
    public class CatalogUnitOfWork : ICatalogUnitOfWork
    {
        private readonly CatalogDbContext _context;

        // Bơm DbContext của riêng module Identity vào đây
        public CatalogUnitOfWork(CatalogDbContext context)
        {
            _context = context;
        }

        // Thực thi hàm SaveChangesAsync được thừa kế từ IUnitOfWork gốc
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Ủy quyền việc lưu trữ thực sự cho Entity Framework Core
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
