namespace Orders.Application.Interfaces.ExternalServices;

public record StoreTableDto(
    Guid TableId,
    string TableName,
    int SeatCapacity,
    bool IsActive // Bàn có đang được sử dụng hay bị hỏng/đóng?
);

public interface IStoreTableService
{
    // Cần cả StoreId để tránh việc khách ở quán A lại quét mã QR chọn bàn của quán B
    Task<StoreTableDto?> GetTableAsync(Guid storeId, Guid tableId, CancellationToken cancellationToken);
}