namespace Orders.Application.Interfaces.ExternalServices;

// DTO định nghĩa chính sách giao hàng
public record StoreDeliveryPolicyDto(
    double Latitude,
    double Longitude,
    double DeliveryRadiusKm, // Bán kính tối đa cho phép giao
    decimal BaseShippingFee, // Phí nền (VD: 15.000đ cho 3km đầu)
    decimal ShippingFeePerKm, // Phí cộng thêm (VD: 5.000đ/km tiếp theo)
    string Currency // Tiền tệ (VD: VND)
);

public interface IStoreDeliveryPolicyService
{
    // StoreId ở Order đang lưu là Guid (tương ứng với PublicId bên Store)
    Task<StoreDeliveryPolicyDto?> GetDeliveryPolicyAsync(
        Guid storeId,
        CancellationToken cancellationToken);
}