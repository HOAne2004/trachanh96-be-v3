using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.ValueObjects;

namespace Stores.Infrastructure.Database.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Domain.Entities.Store>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Store> builder)
    {
        builder.ToTable("Stores", "store");

        builder.HasKey(x => x.Id);

        // 1. CẤU HÌNH CỘT (Properties)
        builder.Property(x => x.StoreCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Slug)
            .HasConversion(slug => slug.Value, value => Slug.Create(value))
            .HasMaxLength(200)
            .IsRequired();

        builder.ComplexProperty(x => x.BaseShippingFee, fee =>
        {
            fee.Property(p => p.Amount).HasColumnName("BaseShippingFeeAmount");
            fee.Property(p => p.Currency).HasColumnName("BaseShippingFeeCurrency").HasMaxLength(10);
        });

        builder.ComplexProperty(x => x.ShippingFeePerKm, fee =>
        {
            fee.Property(p => p.Amount).HasColumnName("ShippingFeePerKmAmount");
            fee.Property(p => p.Currency).HasColumnName("ShippingFeePerKmCurrency").HasMaxLength(10);
        });

        // 2. CẤU HÌNH RÀNG BUỘC DUY NHẤT (Unique Indexes)
        // Đây là cách đúng để tạo Unique Constraint trong EF Core
        builder.HasIndex(x => x.StoreCode).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();

        // 3. GLOBAL QUERY FILTER (Tự động bỏ qua các record đã bị Soft Delete)
        builder.HasQueryFilter(x => !x.IsDeleted);

        // 4. CẤU HÌNH QUAN HỆ 1-N (AGGREGATE ROOT)
        builder.HasMany(x => x.OperatingHours)
            .WithOne()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Domain.Entities.Store.OperatingHours))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Areas)
            .WithOne()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Domain.Entities.Store.Areas))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}