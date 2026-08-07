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

}
