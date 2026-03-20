using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Tên bảng
        builder.ToTable("Users");

        // Khóa chính
        builder.HasKey(x => x.Id);

        // Chỉ mục (Index) hỗ trợ tìm kiếm và Unique
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();

        // Cấu hình các cột cơ bản
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.PasswordHash).IsRequired();

        // 1. DẠY EF CORE CÁCH MAP VALUE OBJECT (PhoneNumber)
        builder.Property(x => x.Phone)
            .HasConversion(
                phoneNumber => phoneNumber != null ? phoneNumber.Value : null,
                dbValue => dbValue != null ? PhoneNumber.Create(dbValue) : null
            )
            .HasMaxLength(20);

        // DẠY EF CORE CÁCH MAP VALUE OBJECT (EmailAddress)
        // DẠY EF CORE CÁCH MAP VALUE OBJECT (EmailAddress)
        builder.Property(x => x.Email)
            .HasConversion(
                email => email.Value,                     // 1. Khi lưu xuống DB: Lấy chuỗi string ra
                dbValue => EmailAddress.Create(dbValue)   // 2. Khi đọc lên: Đóng gói lại thành Value Object
            )
            .IsRequired()
            .HasMaxLength(255);

        // 2. DẠY EF CORE CÁCH MAP DANH SÁCH BỊ ĐÓNG GÓI (_addresses)
        builder.Metadata.FindNavigation(nameof(User.Addresses))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Cấu hình Relationship 1-N giữa User và Address
        builder.HasMany(x => x.Addresses)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade); // Nếu Hard Delete User thì xóa luôn Address

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}