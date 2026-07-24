using AI.Domain.Enums;

namespace AI.Domain.Entities
{
    public class ChatMessage
    {
        public Guid Id { get; private set; }
        public Guid ChatSessionId { get; private set; } // Chuẩn hóa tham chiếu về Session
        public MessageRoleEnum Role { get; private set; }
        public string Content { get; private set; }
        public string? Payload { get; private set; } // Dùng cho Generative UI (JSON chứa list món, URL hình ảnh, action,...)
        public DateTime Timestamp { get; private set; }

        // Quản lý Token & Trạng thái
        public int PromptTokens { get; private set; }
        public int CompletionTokens { get; private set; }
        public MessageStatusEnum Status { get; private set; }
        public string? ErrorMessage { get; private set; }

        protected ChatMessage()
        {
            Content = string.Empty;
        }

        public ChatMessage(
            Guid sessionId,
            MessageRoleEnum role,
            string content,
            MessageStatusEnum status = MessageStatusEnum.Success,
            string? payload = null,
            int promptTokens = 0,
            int completionTokens = 0)
        {
            Id = Guid.NewGuid();
            ChatSessionId = sessionId;
            Role = role;
            Content = content ?? string.Empty;
            Status = status;
            Payload = payload;
            PromptTokens = promptTokens;
            CompletionTokens = completionTokens;
            Timestamp = DateTime.UtcNow;
        }

        // Cập nhật kết quả phản hồi thành công từ AI
        public void Complete(string content, string? payload = null, int promptTokens = 0, int completionTokens = 0)
        {
            Content = content;
            Payload = payload;
            PromptTokens = promptTokens;
            CompletionTokens = completionTokens;
            Status = MessageStatusEnum.Success;
        }

        // Đánh dấu lỗi khi gọi AI thất bại (Timeout, 500,...)
        public void MarkAsFailed(string errorMessage)
        {
            Status = MessageStatusEnum.Failed;
            ErrorMessage = errorMessage;
        }

        // Đánh dấu bị bộ lọc Gemini chặn (Safety trigger)
        public void MarkAsBlocked(string reason)
        {
            Status = MessageStatusEnum.Blocked;
            ErrorMessage = reason;
        }
    }
}