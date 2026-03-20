using Microsoft.EntityFrameworkCore;
using Stores.Application.Interfaces;
using Stores.Infrastructure.Database;
public class StoreRepository : IStoreRepository
{
    private readonly StoreDbContext _context;
    public StoreRepository(StoreDbContext context) => _context = context;

    public async Task<Stores.Domain.Entities.Store?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Stores.FirstOrDefaultAsync(s => s.PublicId == publicId, cancellationToken);
    }

    public async Task<Stores.Domain.Entities.Store?> GetAggregateAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Stores
            .Include(s => s.OperatingHours)
            .Include(s => s.Areas)
                .ThenInclude(a => a.Tables)
            .FirstOrDefaultAsync(s => s.PublicId == publicId, cancellationToken);
    }

    public async Task<bool> ExistsByStoreCodeAsync(string storeCode, CancellationToken cancellationToken = default)
    {
        return await _context.Stores.AnyAsync(s => s.StoreCode == storeCode, cancellationToken);
    }

    public void Add(Stores.Domain.Entities.Store store) => _context.Stores.Add(store);
}