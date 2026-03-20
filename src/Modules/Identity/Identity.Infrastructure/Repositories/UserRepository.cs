using Identity.Application.Interfaces;
using Identity.Domain.Entities;
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

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
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
    }
}
