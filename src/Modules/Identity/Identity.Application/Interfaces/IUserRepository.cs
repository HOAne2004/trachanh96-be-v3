using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);


}
