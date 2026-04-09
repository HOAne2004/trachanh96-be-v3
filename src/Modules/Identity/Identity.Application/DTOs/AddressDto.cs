
namespace Identity.Application.DTOs
{
    public record AddressDto(
    int Id,
    string RecipientName,
    string PhoneNumber,
    string AddressDetail,
    string Province,
    string District,
    string Commune,
    string FullAddress, // Lấy thẳng cái Computed Property từ Domain cực kỳ tiện lợi
    double? Latitude,
    double? Longitude,
    bool IsDefault
);
}
