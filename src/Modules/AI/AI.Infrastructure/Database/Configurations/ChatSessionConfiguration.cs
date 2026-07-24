using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Database.Configurations
{
    public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
    {
        public void Configure(EntityTypeBuilder<ChatSession> builder)
        {
            // Định nghĩa bảng nằm trong schema "ai"
            builder.ToTable("ChatSessions", "ai");

            // Khóa chính độc lập
            builder.HasKey(x => x.Id);

            // Khóa ngoại tùy chọn (Nullable)
            builder.Property(x => x.UserId)
                .IsRequired(false);

            builder.Property(x => x.CurrentOrderId)
                .IsRequired(false);

            builder.Property(x => x.CurrentReservationId)
                .IsRequired(false);

            // Quản lý trạng thái & Token
            builder.Property(x => x.IsLocked)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.TotalTokensUsed)
                .IsRequired()
                .HasDefaultValue(0);

            // Thời gian
            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            // Quan hệ 1 - N giữa ChatSession và ChatMessage
            builder.HasMany(x => x.Messages)
                .WithOne()
                .HasForeignKey(x => x.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bắt buộc cấu hình Field Access Mode để EF Core truy cập đúng backing field private '_messages'
            builder.Metadata.FindNavigation(nameof(ChatSession.Messages))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}