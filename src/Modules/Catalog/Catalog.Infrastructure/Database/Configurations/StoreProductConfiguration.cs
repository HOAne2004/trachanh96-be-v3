using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Database.Configurations;

public class StoreProductConfiguration : IEntityTypeConfiguration<StoreProduct>
{
    public void Configure(EntityTypeBuilder<StoreProduct> builder)
    {
        // Đặt tên bảng và schema riêng của module Catalog
        builder.ToTable("StoreProducts", "catalog");

        // 1. KHÓA CHÍNH KẾT HỢP (Composite Key)
        // Mỗi món ăn trong 1 cửa hàng là duy nhất
        builder.HasKey(x => new { x.StoreId, x.ProductId });

        // 2. CẤU HÌNH CỘT (Properties)
        builder.Property(x => x.PriceOverride)
               .HasColumnType("decimal(18,2)")
               .IsRequired(false); // Có thể null (nếu null thì dùng giá mặc định của Product)

        builder.Property(x => x.IsAvailable)
               .HasDefaultValue(true);

        builder.Property(x => x.IsActive)
               .HasDefaultValue(true);

        // 3. CẤU HÌNH QUAN HỆ (CHỈ VỚI PRODUCT)
        builder.HasOne(sp => sp.Product)
            .WithMany(p => p.StoreProducts) // Map với IReadOnlyCollection vừa thêm ở Bước 1
            .HasForeignKey(sp => sp.ProductId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa Product thì xóa luôn dữ liệu ở bảng này

        // 🚨 QUAN TRỌNG: KHÔNG CẤU HÌNH HasOne() với Store.
        // StoreId ở đây chỉ là một GUID thuần túy để filter.
    }
}