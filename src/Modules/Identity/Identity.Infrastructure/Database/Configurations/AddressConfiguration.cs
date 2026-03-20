using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Database.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        // 1. Tên bảng và Khóa chính
        builder.ToTable("Addresses");
        builder.HasKey(x => x.Id);

        // 2. Cấu hình các cột cơ bản (Đồng bộ độ dài với Rule Validation ở Entity)
        builder.Property(x => x.RecipientName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.AddressDetail).IsRequired().HasMaxLength(300);

        // Tỉnh/Huyện/Xã thường không dài quá 100 ký tự
        builder.Property(x => x.Province).IsRequired().HasMaxLength(100);
        builder.Property(x => x.District).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Commune).IsRequired().HasMaxLength(100);

        // 3. Dạy EF Core cách Map Value Object (PhoneNumber)
        builder.Property(x => x.RecipientPhone)
            .HasConversion(
                phoneNumber => phoneNumber!.Value,
                dbValue => PhoneNumber.Create(dbValue!)
            )
            .IsRequired()
            .HasMaxLength(20);

        // 4. Tọa độ (Có thể Null)
        builder.Property(x => x.Latitude).IsRequired(false);
        builder.Property(x => x.Longitude).IsRequired(false);

        // 5. BỎ QUA Computed Property
        // Báo cho EF Core biết FullAddress chỉ dùng trong C#, không được tạo cột trong DB
        builder.Ignore(x => x.FullAddress);

        // 6. Global Query Filter (Tự động lọc Xóa Mềm)
        // Bất kỳ câu query nào truy vấn bảng Address đều tự động thêm điều kiện "WHERE IsDeleted = false"
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}