using Catalog.Domain.Entities;

namespace Catalog.Application.Interfaces
{
    public interface IToppingRepository
    {
        Task<Topping?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
        void Add(Topping topping);
    }
}
