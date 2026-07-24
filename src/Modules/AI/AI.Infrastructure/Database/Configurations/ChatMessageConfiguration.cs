using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Database.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages", "ai");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ChatSessionId)
                .IsRequired();

            // Lưu Enum dưới dạng chuỗi (User / Model / System) trong DB
            builder.Property(x => x.Role)
                .IsRequired()
                .HasConversion<string>();

            // Giới hạn độ dài nội dung để tránh spam DB, đồng thời tối ưu bộ nhớ
            builder.Property(x => x.Content)
                .IsRequired()
                .HasMaxLength(1000);

            // Cấu hình Payload chứa dữ liệu cấu trúc (JSON list món, ảnh, function call result...)
            // Map trực tiếp thành kiểu jsonb trong PostgreSQL để truy vấn siêu tốc
            builder.Property(x => x.Payload)
                .IsRequired(false)
                .HasColumnType("jsonb");

            // Quản lý Token tiêu thụ cho từng tin nhắn
            builder.Property(x => x.PromptTokens)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CompletionTokens)
                .IsRequired()
                .HasDefaultValue(0);

            // Lưu Enum trạng thái tin nhắn dưới dạng chuỗi (Processing / Success / Failed / Blocked)
            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(x => x.ErrorMessage)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.Timestamp)
                .IsRequired();
        }
    }
}