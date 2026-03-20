using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stores.Domain.Entities;

namespace Stores.Infrastructure.Database.Configurations;

public class StoreOperatingHourConfiguration : IEntityTypeConfiguration<StoreOperatingHour>
{
    public void Configure(EntityTypeBuilder<StoreOperatingHour> builder)
    {
        builder.ToTable("StoreOperatingHours", "store");
        builder.HasKey(x => x.Id);
    }
}