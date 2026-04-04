namespace Orders.Application.Interfaces.ExternalServices;

// DTO định nghĩa đúng những gì Order cần
public record UserAddressDto(
    string RecipientName,
    string PhoneNumber,
    string FullAddress, // Đã nối sẵn số nhà, phường, xã, tỉnh...
    double? Latitude,
    double? Longitude
);

public interface IUserAddressService
{
    // Truyền vào userId để bảo mật (đảm bảo khách không truyền bừa AddressId của người khác)
    Task<UserAddressDto?> GetUserAddressAsync(
        Guid userId,
        int addressId, // Entity Address của bạn đang dùng int
        CancellationToken cancellationToken);
}