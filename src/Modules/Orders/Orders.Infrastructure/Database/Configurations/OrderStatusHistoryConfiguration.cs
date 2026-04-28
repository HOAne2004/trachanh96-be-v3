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

            builder.Property(x => x.Reason).HasMaxLength(500);
            builder.Property(x => x.ChangedAt).IsRequired();

            // ==========================================
            // [CẬP NHẬT] LƯU ENUM DƯỚI DẠNG STRING
            // ==========================================
            builder.Property(x => x.ToStatus)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(x => x.FromStatus)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired(false); // Được phép Null vì lúc tạo đơn mới (Draft) thì FromStatus = null
        }
    }
}