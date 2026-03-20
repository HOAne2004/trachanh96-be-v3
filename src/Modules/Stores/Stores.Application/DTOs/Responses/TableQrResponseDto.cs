
namespace Stores.Application.DTOs.Responses
{
    public record TableQrResponseDto(
        Guid StorePublicId,
        string StoreName,
        int AreaId,
        string AreaName,
        int TableId,
        string TableName,
        int SeatCapacity
    );
}
