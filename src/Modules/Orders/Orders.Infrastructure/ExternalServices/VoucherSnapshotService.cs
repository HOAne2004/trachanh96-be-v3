using Orders.Application.Interfaces.ExternalServices;
using Shared.Domain.Enums;

namespace Orders.Infrastructure.ExternalServices;

public class VoucherSnapshotService : IVoucherSnapshotService
{
    public Task<VoucherSnapshotDto?> GetValidVoucherAsync(string voucherCode, CancellationToken cancellationToken)
    {
        var code = voucherCode.Trim().ToUpper();

        // Giả lập: Cứ nhập GIAM10K là được trừ thẳng 10.000đ cho đơn từ 50.000đ
        if (code == "GIAM10K")
        {
            return Task.FromResult<VoucherSnapshotDto?>(new VoucherSnapshotDto(
                "GIAM10K", DiscountTypeEnum.FixedAmount, 10000, null, 50000));
        }

        // Giả lập: Nhập SALE20 được giảm 20%, tối đa 30k
        if (code == "SALE20")
        {
            return Task.FromResult<VoucherSnapshotDto?>(new VoucherSnapshotDto(
                "SALE20", DiscountTypeEnum.Percentage, 20, 30000, 0));
        }

        // Nhập sai thì báo null
        return Task.FromResult<VoucherSnapshotDto?>(null);
    }
}