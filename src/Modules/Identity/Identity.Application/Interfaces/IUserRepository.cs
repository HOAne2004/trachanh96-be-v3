using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    void Add(User user);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetPaginatedAsync(
    int pageIndex, int pageSize, string? searchTerm, string? role, string? status, CancellationToken cancellationToken);

    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);


}
