using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.ValueObjects;

namespace Catalog.Infrastructure.Database.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PublicId).IsUnique();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(255);

        builder.Property(x => x.Slug)
            .HasConversion(slug => slug.Value, value => Slug.CreateManual(value))
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.OwnsOne(x => x.BasePrice, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("BasePrice_Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("BasePrice_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // MAP ENUMS THÀNH STRING
        builder.Property(x => x.ProductType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // CẤU HÌNH QUAN HỆ 1-N VỚI SIZES VÀ TOPPINGS
        builder.HasMany(x => x.ProductSizes)
            .WithOne()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa sản phẩm -> Bay luôn cấu hình size

        builder.HasMany(x => x.ProductToppings)
            .WithOne()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Liên kết với Category (Không cho phép cascade delete)
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(p => p.PublishedAt)
            .IsRequired(false); // Cho phép Null vì sản phẩm Draft chưa có ngày ra mắt
        builder.Metadata.FindNavigation(nameof(Product.StoreProducts))!
           .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}