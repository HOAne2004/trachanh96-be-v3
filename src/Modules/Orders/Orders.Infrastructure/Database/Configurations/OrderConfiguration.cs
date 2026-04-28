using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Database.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders", "orders");
            builder.HasKey(x => x.Id);

            // 1. Index quan trọng cho Production
            builder.HasIndex(x => x.OrderCode).IsUnique();
            builder.HasIndex(x => new { x.StoreId, x.OrderStatus, x.CreatedAt });
            builder.HasIndex(x => new { x.CustomerId, x.CreatedAt });

            // 2. Map các thuộc tính cơ bản
            builder.Property(x => x.OrderCode).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();

            // ==========================================
            // [CẬP NHẬT] LƯU ENUM DƯỚI DẠNG STRING
            // ==========================================
            builder.Property(x => x.OrderStatus).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.OrderType).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.VoucherDiscountType).HasConversion<string>().HasMaxLength(30);

            // 3. Concurrency Token (Chống Race Condition)
            builder.Property(x => x.RowVersion).IsRequired();

            // 4. Map Value Object Money bằng ComplexProperty (Hoặc OwnsOne)
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

            builder.OwnsOne(x => x.DeliveryDetails);

            builder.OwnsOne(x => x.ShippingFee, p =>
            {
                p.Property(m => m.Amount).HasColumnName("ShippingFeeAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("ShippingFeeCurrency").HasMaxLength(3);
            });

            // 5. Cấu hình Relationship
            builder.HasMany(x => x.Items)
                   .WithOne()
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StatusHistories)
                   .WithOne()
                   .HasForeignKey("OrderId") // Shadow Foreign Key
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}