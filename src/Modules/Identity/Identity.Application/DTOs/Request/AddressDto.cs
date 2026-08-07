
namespace Identity.Application.DTOs.Request
{
    public record AddressDto(
        Guid Id,
        string RecipientName,
        string Phone,
        string FullAddress,
        string AddressDetail,
        string Province,
        string District,
        string Commune,
        double? Latitude,
        double? Longitude,
        bool IsDefault
    );
}
