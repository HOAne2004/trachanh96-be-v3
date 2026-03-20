using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.ValueObjects;

namespace Catalog.Infrastructure.Database.Configurations;

public class ToppingConfiguration : IEntityTypeConfiguration<Topping>
{
    public void Configure(EntityTypeBuilder<Topping> builder)
    {
        builder.ToTable("Toppings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(255);

        builder.Property(x => x.Slug)
            .HasConversion(s => s.Value, v => Slug.CreateManual(v))
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        // Tách object Money thành 2 cột
        builder.OwnsOne(x => x.BasePrice, p => {
            p.Property(m => m.Amount).HasColumnName("BasePrice_Amount").HasPrecision(18, 2).IsRequired();
            p.Property(m => m.Currency).HasColumnName("BasePrice_Currency").HasMaxLength(3).IsRequired();
        });
    }
}