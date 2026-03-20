using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Shared.Domain.ValueObjects;

namespace Catalog.Application.Interfaces
{
    public interface IProductRepository
    {

        Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Product>> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string name, int? categoryId, int? excludeId = null, CancellationToken cancellationToken = default);
        Task<Product?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
        Task<Product?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default);
        void Add(Product product);

        Task<(List<Product> Items, int TotalCount)> GetPagedListAsync(
    string? searchTerm, int? categoryId, ProductTypeEnum? type, List<ProductStatusEnum>? statuses,
    DateTime? fromDate, DateTime? toDate,
    int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    }
}
