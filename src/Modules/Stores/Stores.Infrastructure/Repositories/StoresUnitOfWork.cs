using Stores.Application.Interfaces;
using Stores.Infrastructure.Database;

namespace Stores.Infrastructure.Repositories
{
    public class StoresUnitOfWork : IStoresUnitOfWork
    {
        private readonly StoreDbContext _context;

        // Bơm DbContext của riêng module Identity vào đây
        public StoresUnitOfWork(StoreDbContext context)
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
