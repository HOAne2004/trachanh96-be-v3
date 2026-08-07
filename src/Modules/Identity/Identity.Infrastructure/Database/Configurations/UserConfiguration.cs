using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // --- Value Object Mapping ---
        // Mapping EmailAddress Value Object thành cột string trong DB
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value))
            .HasColumnName("Email")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Mapping PhoneNumber Value Object
        builder.Property(u => u.Phone)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => !string.IsNullOrEmpty(value) ? PhoneNumber.Create(value) : null)
            .HasColumnName("Phone")
            .HasMaxLength(20)
            .IsRequired(false);

        // --- Basic Properties ---
        builder.Property(u => u.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.ThumbnailUrl)
            .HasMaxLength(1000);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // --- Verification & Security ---
        builder.Property(u => u.VerificationToken)
            .HasMaxLength(100);

        // --- Audit Properties (Từ Base AuditableEntity) ---
        builder.Property(u => u.CreatedBy).HasMaxLength(100);
        builder.Property(u => u.LastModifiedBy).HasMaxLength(100);
        builder.Property(u => u.DeletedBy).HasMaxLength(100);

        // --- Relationships Configuration (Dùng Backing Fields) ---
        // 1. Address Navigation
        builder.HasMany(u => u.Addresses)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.Addresses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // 2. UserSession Navigation
        builder.HasMany(u => u.Sessions)
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.Sessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // 3. UserRole Navigation
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Query Filter cho Soft Delete
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}