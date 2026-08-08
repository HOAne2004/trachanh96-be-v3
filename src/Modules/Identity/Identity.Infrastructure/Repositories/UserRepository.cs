using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

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
        return await _context.Users.AnyAsync(u => u.Email == emailAddress && !u.IsDeleted, cancellationToken);
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailAddress = EmailAddress.Create(email);
        return await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Email == emailAddress && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Addresses)
            .Include(u => u.UserRoles)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        // User trong hầu hết trường hợp đã được tracking sẵn (load qua GetByIdAsync trong cùng scope),
        // Change Tracker tự phát hiện đúng property đã đổi. Chỉ gọi Update() khi entity thực sự Detached
        // (trường hợp hiếm, ví dụ entity được tái tạo từ nguồn khác) để tránh ép TOÀN BỘ cột về Modified.
        if (_context.Entry(user).State == EntityState.Detached)
        {
            _context.Users.Update(user);
        }
        return Task.CompletedTask;
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetPaginatedAsync(
        int pageIndex, int pageSize, string? searchTerm, Guid? roleId, string? status, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(search)
                                  || u.Email.Value.ToLower().Contains(search));
        }

        if (roleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatusEnum>(status, true, out var statusEnum))
        {
            query = query.Where(u => u.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    // Tìm User chứa Session có mã Hash khớp
    public async Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Sessions.Any(s =>
                s.RefreshTokenHash == refreshTokenHash
                && !s.IsRevoked
                && s.ExpiryDate > DateTime.UtcNow), cancellationToken); // Bổ sung: loại session đã hết hạn nhưng chưa từng bị Revoke()
    }

    public async Task<User?> GetByIdWithDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .IgnoreQueryFilters() // Bắt buộc: bỏ qua Global Query Filter (!IsDeleted) để tìm được cả User đã xóa mềm
            .Include(u => u.UserRoles)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Guid?> GetSecurityStampAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => (Guid?)u.SecurityStamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUsersInRoleAsync(Guid roleId, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        // Dựa trên _context.Users nên Global Query Filter (!IsDeleted) tự động áp dụng
        return await _context.Users
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
            .Where(u => excludeUserId == null || u.Id != excludeUserId.Value)
            .CountAsync(cancellationToken);
    }
}