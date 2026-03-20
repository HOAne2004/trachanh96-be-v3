using Identity.Application.Interfaces;

namespace Identity.Infrastructure.Database;

public class IdentityUnitOfWork : IIdentityUnitOfWork
{
    private readonly IdentityDbContext _context;

    public IdentityUnitOfWork(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}