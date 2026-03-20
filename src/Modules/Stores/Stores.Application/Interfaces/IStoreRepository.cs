using Stores.Domain.Entities;
namespace Stores.Application.Interfaces;

public interface IStoreRepository
{
    Task<Store?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<Store?> GetAggregateAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByStoreCodeAsync(string storeCode, CancellationToken cancellationToken = default);

    void Add(Store store);
}