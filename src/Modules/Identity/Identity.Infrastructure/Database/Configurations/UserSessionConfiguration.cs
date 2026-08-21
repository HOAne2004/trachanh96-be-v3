using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Identity.Domain.Entities;

namespace Identity.Infrastructure.Database.Configurations
{
    public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.ToTable("UserSessions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id).ValueGeneratedNever(); // Id do Domain tự sinh (Guid.CreateVersion7),
                                                               // không phải do DB - bắt buộc khai báo tường minh
                                                               // để EF Core không hiểu nhầm entity mới là "đã tồn tại"
                                                               // khi nó được phát hiện qua collection navigation
                                                               // (AddSession) thay vì gọi Add() trực tiếp.

            builder.Property(s => s.RefreshTokenHash)
                .HasMaxLength(500)
                .IsRequired();

            // Đánh index cho RefreshTokenHash để query khi gia hạn Token
            builder.HasIndex(s => s.RefreshTokenHash);

            builder.Property(s => s.DeviceName)
                .HasMaxLength(255);

            builder.Property(s => s.IpAddress)
                .HasMaxLength(45);

            builder.Property(s => s.CreatedBy).HasMaxLength(100);
            builder.Property(s => s.LastModifiedBy).HasMaxLength(100);
            builder.Property(s => s.DeletedBy).HasMaxLength(100);
        }
    }
}
