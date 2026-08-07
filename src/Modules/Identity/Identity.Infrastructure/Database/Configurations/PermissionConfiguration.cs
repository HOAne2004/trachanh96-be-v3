using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        // Id chính là Permission Code (string)
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasMaxLength(150)
            .ValueGeneratedNever(); // Không tự tăng, Id do Developer truyền vào

        builder.Property(p => p.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Module)
            .HasMaxLength(100)
            .IsRequired();

        // Đánh Index theo Module để Admin UI load danh sách quyền theo tab cực nhanh
        builder.HasIndex(p => p.Module);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.IsSystem)
            .IsRequired();

        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.LastModifiedBy).HasMaxLength(100);
        builder.Property(p => p.DeletedBy).HasMaxLength(100);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}