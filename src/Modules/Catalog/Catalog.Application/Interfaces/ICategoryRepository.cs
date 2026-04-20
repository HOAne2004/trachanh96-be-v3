using Catalog.Domain.Entities;

namespace Catalog.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string name, int? parentId, int? excludeId = null, CancellationToken cancellationToken = default);
        Task<bool> HasChildrenAsync(int categoryId, CancellationToken cancellationToken = default);
        Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);
        void Add(Category category);
    }
}
