using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Database.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems", "orders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(500);

            // ==========================================
            // [CẬP NHẬT] ENUM DẠNG STRING (Bạn đã làm đúng phần này)
            // ==========================================
            builder.Property(x => x.SizeName).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.IceLevel).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.SugarLevel).HasConversion<string>().HasMaxLength(20);

            // 1. Dùng OwnsOne thay cho ComplexProperty
            builder.OwnsOne(x => x.UnitPrice, p =>
            {
                p.Property(m => m.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
            });

            builder.OwnsOne(x => x.TotalPrice, p =>
            {
                p.Property(m => m.Amount).HasColumnName("TotalPriceAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("TotalPriceCurrency").HasMaxLength(3);
            });

            // 2. JSONB COLUMN
            builder.OwnsMany(x => x.Toppings, t =>
            {
                t.ToJson();
                t.Property(x => x.ToppingName).HasMaxLength(255).IsRequired();

                t.OwnsOne(x => x.Price, p =>
                {
                    p.Property(m => m.Amount);
                    p.Property(m => m.Currency);
                });
            });
        }
    }
}