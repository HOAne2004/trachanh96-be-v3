namespace Stores.Application.DTOs.Responses
{
    public record StoreAdminListDto(
    Guid PublicId,
    string StoreCode,
    string Name,
    string Status,
    string FullAddress
);
}
