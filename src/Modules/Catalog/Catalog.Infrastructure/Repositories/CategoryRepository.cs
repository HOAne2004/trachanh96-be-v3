using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly CatalogDbContext _context;

    public CategoryRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? parentId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories
            .Where(x => x.Name == name && x.ParentId == parentId && !x.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(x => x.ParentId == categoryId && !x.IsDeleted, cancellationToken);
    }

    public async Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
    public void Add(Category category)
    {
        _context.Categories.Add(category);
    }
}