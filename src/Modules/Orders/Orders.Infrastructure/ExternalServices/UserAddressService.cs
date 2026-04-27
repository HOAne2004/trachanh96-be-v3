using Orders.Application.Interfaces.ExternalServices;

namespace Orders.Infrastructure.ExternalServices;

public class UserAddressService : IUserAddressService
{
    public Task<UserAddressDto?> GetUserAddressAsync(Guid userId, int addressId, CancellationToken cancellationToken)
    {
        // Giả lập trả về địa chỉ hợp lệ tại Hà Nội
        return Task.FromResult<UserAddressDto?>(new UserAddressDto(
            "Nguyễn Văn A (Khách Test)",
            "0901234567",
            "Số 1 Đại Cồ Việt, Bách Khoa, Hai Bà Trưng, Hà Nội",
            21.006399, // Latitude Đại Cồ Việt
            105.842793 // Longitude Đại Cồ Việt
        ));
    }
}