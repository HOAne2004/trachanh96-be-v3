using Identity.Application.Interfaces;
using Identity.Infrastructure.Database;

namespace Identity.Infrastructure.Repositories
{
    public class IdentityUnitOfWork : IIdentityUnitOfWork
    {
        private readonly IdentityDbContext _context;

        // Bơm DbContext của riêng module Identity vào đây
        public IdentityUnitOfWork(IdentityDbContext context)
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
