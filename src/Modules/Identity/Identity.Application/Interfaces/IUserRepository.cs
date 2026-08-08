using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    void Add(User user);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetPaginatedAsync(
        int pageIndex, int pageSize, string? searchTerm, Guid? roleId, string? status, CancellationToken cancellationToken);
    Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid?> GetSecurityStampAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Đếm số User (chưa xóa mềm) đang giữ 1 Role cụ thể, có thể loại trừ 1 UserId khỏi phép đếm.
    /// Dùng để kiểm tra "nếu gỡ Role này khỏi User X, còn ai khác giữ Role đó không" - chống việc
    /// gỡ Role hệ thống (ADMIN/SUPER_ADMIN) khỏi người giữ cuối cùng.
    /// </summary>
    Task<int> CountUsersInRoleAsync(Guid roleId, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải User kèm UserRoles cho mục đích hiển thị Profile - KHÔNG Include Addresses/Sessions
    /// (không cần thiết cho màn hình profile), giảm 2 JOIN không cần thiết so với GetByIdAsync.
    /// </summary>
    Task<User?> GetProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
