using AI.Domain.Enums;

namespace AI.Domain.Entities
{
    public class ChatSession
    {
        public Guid OrderId { get; private set; } // Khớp với ID của Giỏ hàng
        public DateTime CreatedAt { get; private set; }

        private readonly List<ChatMessage> _messages = new();
        public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

        protected ChatSession() { }

        public ChatSession(Guid orderId)
        {
            OrderId = orderId;
            CreatedAt = DateTime.UtcNow;
        }

        // Hàm chuẩn DDD để thêm tin nhắn vào phiên
        public void AddMessage(MessageRoleEnum role, string content)
        {
            _messages.Add(new ChatMessage(OrderId, role, content));
        }
    }

    public class ChatMessage
    {
        public Guid Id { get; private set; }
        public Guid ChatSessionId { get; private set; }
        public MessageRoleEnum Role { get; private set; }
        public string Content { get; private set; }
        public DateTime Timestamp { get; private set; }

        protected ChatMessage() { Content = string.Empty; }

        public ChatMessage(Guid sessionId, MessageRoleEnum role, string content)
        {
            Id = Guid.NewGuid();
            ChatSessionId = sessionId;
            Role = role;
            Content = content;
            Timestamp = DateTime.UtcNow;
        }
    }
}
