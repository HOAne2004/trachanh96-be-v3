using Orders.Application.Interfaces.ExternalServices;

namespace Orders.Infrastructure.ExternalServices;

public class StoreTableService : IStoreTableService
{
    public Task<StoreTableDto?> GetTableAsync(Guid storeId, Guid tableId, CancellationToken cancellationToken)
    {
        // Giả lập bàn số 5 hợp lệ
        return Task.FromResult<StoreTableDto?>(new StoreTableDto(tableId, "Bàn số 5", 4, true));
    }
}