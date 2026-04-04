using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Database.Configurations
{
    public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
        {
            builder.ToTable("OrderStatusHistories", "orders");
            builder.HasKey(x => x.Id);

            // Cấu hình độ dài cột để DB không bị set mặc định là nvarchar(max) gây nặng máy
            builder.Property(x => x.Reason).HasMaxLength(500);

            // Các thuộc tính bắt buộc (NOT NULL)
            builder.Property(x => x.ToStatus).IsRequired();
            builder.Property(x => x.ChangedAt).IsRequired();

            // Nếu muốn, bạn có thể cấu hình rõ cột FromStatus được phép Null
            builder.Property(x => x.FromStatus).IsRequired(false);
        }
    }
}
