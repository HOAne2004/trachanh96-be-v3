using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Database.Configurations
{
    public class OrderConfiguration:IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders", "orders");
            builder.HasKey(x => x.Id);

            // 1. Index quan trọng cho Production
            builder.HasIndex(x => x.OrderCode).IsUnique(); // Trùng mã đơn là thảm họa

            // Composite Index cho màn hình POS của nhân viên (Tìm theo cửa hàng + trạng thái + thời gian)
            builder.HasIndex(x => new { x.StoreId, x.OrderStatus, x.CreatedAt });

            // Composite Index cho App khách hàng (Tìm lịch sử đơn của tôi)
            builder.HasIndex(x => new { x.CustomerId, x.CreatedAt });

            // 2. Map các thuộc tính cơ bản
            builder.Property(x => x.OrderCode).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();

            // 3. Concurrency Token (Chống Race Condition)
            builder.Property(x => x.RowVersion)
                   .IsRowVersion()
                   .IsRequired();

            // 4. Map Value Object Money bằng ComplexProperty (Tính năng mới chuẩn DDD của EF Core)
            builder.OwnsOne(x => x.SubTotal, p =>
            {
                p.Property(m => m.Amount).HasColumnName("SubTotalAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("SubTotalCurrency").HasMaxLength(3);
            });

            builder.OwnsOne(x => x.DiscountAmount, p =>
            {
                p.Property(m => m.Amount).HasColumnName("DiscountAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("DiscountCurrency").HasMaxLength(3);
            });

            builder.OwnsOne(x => x.FinalTotal, p =>
            {
                p.Property(m => m.Amount).HasColumnName("FinalTotalAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("FinalTotalCurrency").HasMaxLength(3);
            });

            // 5. Cấu hình Relationship
            // OrderItem
            builder.HasMany(x => x.Items)
                   .WithOne()
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa Order -> Xóa Items

            // StatusHistory (Không có class Configuration riêng vì nó quá nhỏ, cấu hình luôn ở đây)
            builder.HasMany(x => x.StatusHistories)
                   .WithOne()
                   .HasForeignKey("OrderId") // Shadow Foreign Key
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
