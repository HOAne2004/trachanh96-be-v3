using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stores.Domain.Entities;

namespace Stores.Infrastructure.Database.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("Areas", "store");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasQueryFilter(x => !x.IsDeleted);
        // 1 Area có nhiều Tables
        builder.HasMany(x => x.Tables)
            .WithOne()
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Area.Tables))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}