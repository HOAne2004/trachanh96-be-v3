using Orders.Application.Interfaces.ExternalServices;

namespace Orders.Infrastructure.ExternalServices;

public class StoreDeliveryPolicyService : IStoreDeliveryPolicyService
{
    public Task<StoreDeliveryPolicyDto?> GetDeliveryPolicyAsync(Guid storeId, CancellationToken cancellationToken)
    {
        // Giả lập: Tọa độ quán (VD: Hồ Hoàn Kiếm), Bán kính 10km, Phí cơ bản 15k, Mỗi km thêm 5k
        return Task.FromResult<StoreDeliveryPolicyDto?>(new StoreDeliveryPolicyDto(
            21.028511, // Tọa độ quán
            105.854165,
            10.0, // Giao tối đa 10km
            15000,
            5000,
            "VND"
        ));
    }
}