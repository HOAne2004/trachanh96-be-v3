using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.Entities;

namespace Payments.Infrastructure.Database.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        // 1. Tên bảng
        builder.ToTable("PaymentTransactions");

        // 2. Khóa chính
        builder.HasKey(x => x.Id);

        // 3. BẢO VỆ 1: Chống spam click (Unique Index)
        // Đảm bảo không thể có 2 giao dịch mang cùng một IdempotencyKey
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();

        // 4. BẢO VỆ 2: Khóa lạc quan (Optimistic Concurrency)
        // Báo cho EF Core biết RowVersion dùng để chặn việc ghi đè dữ liệu cùng lúc
        builder.Property(x => x.RowVersion)
               .IsConcurrencyToken();

        // 5. Tối ưu kiểu dữ liệu (Best Practice cho Production DB)
        builder.Property(x => x.Currency).HasMaxLength(3); // VD: VND, USD
        builder.Property(x => x.OrderCode).HasMaxLength(50);
        builder.Property(x => x.GatewayTransactionId).HasMaxLength(100); // Mã VNPay/Momo trả về
    }
}