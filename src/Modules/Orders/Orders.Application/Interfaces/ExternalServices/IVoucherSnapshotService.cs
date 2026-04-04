using Shared.Domain.Enums;
namespace Orders.Application.Interfaces.ExternalServices;

public record VoucherSnapshotDto(
    string VoucherCode,
    DiscountTypeEnum DiscountType,
    decimal DiscountValue,   // Số tiền hoặc Số %
    decimal? MaxDiscountAmount, // Cấp trần giảm giá (Nếu là phần trăm)
    decimal MinOrderValue    // Đơn tối thiểu để được áp dụng
);

public interface IVoucherSnapshotService
{
    // Truyền mã Voucher vào, nếu mã sai/hết hạn thì quăng Exception hoặc trả về null
    Task<VoucherSnapshotDto?> GetValidVoucherAsync(string voucherCode, CancellationToken cancellationToken);
}