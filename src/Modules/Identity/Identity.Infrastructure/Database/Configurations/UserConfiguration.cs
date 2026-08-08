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

        // Mới: email đang chờ xác nhận OTP (mô hình 2 bước). Không cần unique constraint ở DB
        // vì chỉ là giá trị tạm, ngắn hạn - tính duy nhất được kiểm tra ở tầng Application
        // ngay trước khi Confirm (xem ConfirmChangeEmailCommandHandler).
        builder.Property(u => u.PendingEmail)
            .HasMaxLength(255);

        builder.Property(u => u.Phone)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => !string.IsNullOrEmpty(value) ? PhoneNumber.Create(value) : null)
            .HasColumnName("Phone")
            .HasMaxLength(20)
            .IsRequired(false);

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

        builder.Property(u => u.VerificationToken)
            .HasMaxLength(100);

        builder.Property(u => u.CreatedBy).HasMaxLength(100);
        builder.Property(u => u.LastModifiedBy).HasMaxLength(100);
        builder.Property(u => u.DeletedBy).HasMaxLength(100);

        builder.HasMany(u => u.Addresses)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.Addresses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(u => u.Sessions)
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.Sessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}