using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Database.Configurations;

public class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        builder.ToTable("ProductSizes");

        // Khóa chính kết hợp
        builder.HasKey(x => new { x.ProductId, x.Size });

        builder.Property(x => x.Size)
            .HasConversion<string>()
            .HasMaxLength(10);

        // Map Value Object Money cho PriceOverride
        builder.OwnsOne(x => x.PriceOverride, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("PriceOverride_Amount")
                .HasPrecision(18, 2);

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("PriceOverride_Currency")
                .HasMaxLength(3);
        });
    }
}