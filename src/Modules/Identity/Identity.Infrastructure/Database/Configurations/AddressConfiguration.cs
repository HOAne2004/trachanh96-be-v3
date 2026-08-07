using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.RecipientName)
            .HasMaxLength(150)
            .IsRequired();

        // Value Object PhoneNumber
        builder.Property(a => a.RecipientPhone)
            .HasConversion(
                phone => phone.Value,
                value => PhoneNumber.Create(value))
            .HasColumnName("RecipientPhone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.AddressDetail)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.Province)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.District)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Commune)
            .HasMaxLength(100)
            .IsRequired();

        // Sử dụng OwnsOne để tách thuộc tính nhưng vẫn lưu cùng bảng Addresses (2 cột Latitude, Longitude)
        builder.OwnsOne(a => a.Location, loc =>
        {
            loc.Property(l => l.Latitude)
               .HasColumnName("Latitude")
               .IsRequired(false);

            loc.Property(l => l.Longitude)
               .HasColumnName("Longitude")
               .IsRequired(false);
        });

        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.LastModifiedBy).HasMaxLength(100);
        builder.Property(a => a.DeletedBy).HasMaxLength(100);

        // Soft Delete Filter
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}