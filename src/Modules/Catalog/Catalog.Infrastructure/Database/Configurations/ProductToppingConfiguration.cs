using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Database.Configurations;

public class ProductToppingConfiguration : IEntityTypeConfiguration<ProductTopping>
{
    public void Configure(EntityTypeBuilder<ProductTopping> builder)
    {
        builder.ToTable("ProductToppings");

        builder.HasKey(x => new { x.ProductId, x.ToppingId });

        builder.Property(x => x.MaxQuantity).HasDefaultValue(1);

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