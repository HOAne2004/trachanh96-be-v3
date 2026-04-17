using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IdentityDbContext _context;
        public UserRepository(IdentityDbContext context)
        {
            _context = context;
        }
        public async Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var emailAddress = EmailAddress.Create(email);

            return await _context.Users
                .AnyAsync(u => u.Email == emailAddress && !u.IsDeleted, cancellationToken);
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var emailAddress = EmailAddress.Create(email);

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == emailAddress && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.PublicId == publicId && !u.IsDeleted, cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
        }

        public async Task<(IEnumerable<User> Users, int TotalCount)> GetPaginatedAsync(
    int pageIndex,
    int pageSize,
    string? searchTerm,
    string? role,
    string? status,
    CancellationToken cancellationToken)
        {
            // 1. Tạo Query gốc (Tối ưu: Dùng AsNoTracking vì ta chỉ ĐỌC dữ liệu, không Update)
            var query = _context.Set<User>().AsNoTracking().AsQueryable();

            // 2. Lọc theo SearchTerm (Tìm theo Email hoặc FullName)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(search)
                                      || u.Email.Value.ToLower().Contains(search));
            }

            // 3. Lọc theo Role
            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRoleEnum>(role, true, out var roleEnum))
            {
                query = query.Where(u => u.Role == roleEnum);
            }

            // 4. Lọc theo Status
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatusEnum>(status, true, out var statusEnum))
            {
                query = query.Where(u => u.Status == statusEnum);
            }

            // 5. Đếm tổng số lượng bản ghi (QUAN TRỌNG: Đếm trước khi Skip/Take)
            var totalCount = await query.CountAsync(cancellationToken);

            // 6. Cắt trang (Pagination) và truy xuất dữ liệu
            var users = await query
                .OrderByDescending(u => u.CreatedAt) // Mới nhất lên đầu
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (users, totalCount);
        }
    }
}
