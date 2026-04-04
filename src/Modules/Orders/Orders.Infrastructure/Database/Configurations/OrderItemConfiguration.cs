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
            builder.Property(x => x.SizeName).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(500);

            // 1. Dùng OwnsOne thay cho ComplexProperty (Tương thích mọi version EF Core)
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

            // ==========================================
            // 2. SỨC MẠNH CỦA POSTGRESQL: JSONB COLUMN
            // ==========================================
            builder.OwnsMany(x => x.Toppings, t =>
            {
                // Lệnh này nói với EF Core: "Hãy nén toàn bộ list Toppings thành cục JSON lưu vào 1 cột trong bảng OrderItems"
                t.ToJson();

                t.Property(x => x.ToppingName).HasMaxLength(255).IsRequired();

                // Map Value Object Money bên trong JSON
                t.OwnsOne(x => x.Price, p =>
                {
                    p.Property(m => m.Amount);
                    p.Property(m => m.Currency);
                });
            });
        }
    }
}
