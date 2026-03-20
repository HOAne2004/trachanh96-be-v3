
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories
{
    public class ToppingRepository : IToppingRepository
    {
        private readonly CatalogDbContext _dbContext;
        public ToppingRepository(CatalogDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        public async Task<Topping?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Toppings
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
        {
                       var query = _dbContext.Toppings
                .Where(t => t.Name == name && !t.IsDeleted);
            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }

        public void Add(Topping topping)
        {
            _dbContext.Toppings.Add(topping);
        }
    }
}
