using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.ValueObjects;

namespace Catalog.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;

    public ProductRepository(CatalogDbContext context)
    {
        _context = context;
    } 

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductSizes)
            .Include(p => p.ProductToppings)
            .ThenInclude(pt => pt.Topping)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<List<Product>> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? categoryId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Where(p => p.Name == name && !p.IsDeleted);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Product?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductSizes)
            .Include(p => p.ProductToppings)
            .ThenInclude(pt => pt.Topping)
            .FirstOrDefaultAsync(p => p.PublicId == publicId && !p.IsDeleted, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductSizes)
            .Include(p => p.ProductToppings)
            .ThenInclude(pt => pt.Topping)
            .FirstOrDefaultAsync(p => p.Slug == slug && !p.IsDeleted, cancellationToken);
    }
    public void Add(Product product)
    {
        _context.Products.Add(product);
    }

    public async Task<(List<Product> Items, int TotalCount)> GetPagedListAsync(
        string? searchTerm, int? categoryId, ProductTypeEnum? type, List<ProductStatusEnum>? statuses,
        DateTime? fromDate, DateTime? toDate,
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        // AsNoTracking để tăng tốc tối đa vì ta chỉ đọc dữ liệu
        var query = _context.Products.AsNoTracking().Where(p => !p.IsDeleted);

        // 1. Lọc theo trạng thái
        if (statuses != null && statuses.Any())
        {
            query = query.Where(p => statuses.Contains(p.Status));
        }

        // 2. Lọc theo loại sản phẩm
        if (type.HasValue)
        {
            query = query.Where(p => p.ProductType == type.Value);
        }

        // 3. Lọc theo danh mục
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // 4. Lọc theo từ khóa tìm kiếm (Name hoặc Slug)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var k = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(k) || p.Slug.Value.ToLower().Contains(k));
        }

        // 5. Lọc theo thời gian
        if (fromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.CreatedAt >= from);
        }
        if (toDate.HasValue)
        {
            var to = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(p => p.CreatedAt < to);
        }

        // Đếm tổng số bản ghi
        var totalCount = await query.CountAsync(cancellationToken);

        // Phân trang và lấy dữ liệu
        var items = await query
            .OrderByDescending(p => p.CreatedAt) // Mặc định mới nhất lên đầu
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}