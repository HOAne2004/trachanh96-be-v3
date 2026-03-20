using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stores.Domain.Entities;

namespace Stores.Infrastructure.Database.Configurations;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("Tables", "store");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.QrCodeToken).HasMaxLength(100).IsRequired();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}