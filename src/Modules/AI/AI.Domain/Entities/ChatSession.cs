using AI.Domain.Enums;

namespace AI.Domain.Entities
{
    public class ChatSession
    {
        public Guid Id { get; private set; }
        public Guid? UserId { get; private set; }
        public Guid? CurrentOrderId { get; private set; }
        public Guid? CurrentReservationId { get; private set; }

        // Quản lý trạng thái phiên
        public bool IsLocked { get; private set; } // Tránh người dùng spam click liên tục khi AI chưa trả lời xong
        public int TotalTokensUsed { get; private set; } // Tổng lượng token tiêu thụ trong session
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<ChatMessage> _messages = new();
        public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

        public int MessageCount => _messages.Count;

        protected ChatSession() { }

        public ChatSession(Guid? userId = null)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            IsLocked = false;
            TotalTokensUsed = 0;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // Đồng bộ tài khoản khi khách ẩn danh đăng nhập
        public void AttachUser(Guid userId)
        {
            UserId = userId;
            UpdatedAt = DateTime.UtcNow;
        }

        // Gắn giỏ hàng/đơn hàng khi bắt đầu luồng đặt món
        public void AttachOrder(Guid orderId)
        {
            CurrentOrderId = orderId;
            UpdatedAt = DateTime.UtcNow;
        }

        // Gắn lịch đặt bàn khi chuyển sang luồng đặt bàn
        public void AttachReservation(Guid reservationId)
        {
            CurrentReservationId = reservationId;
            UpdatedAt = DateTime.UtcNow;
        }

        // Khóa phiên trong lúc đang chờ Gemini xử lý request
        public void LockSession()
        {
            IsLocked = true;
            UpdatedAt = DateTime.UtcNow;
        }

        // Mở khóa phiên sau khi xử lý xong
        public void UnlockSession()
        {
            IsLocked = false;
            UpdatedAt = DateTime.UtcNow;
        }

        // Hàm chuẩn DDD để thêm tin nhắn mới vào phiên
        public ChatMessage AddMessage(
            MessageRoleEnum role,
            string content,
            MessageStatusEnum status = MessageStatusEnum.Success,
            string? payload = null,
            int promptTokens = 0,
            int completionTokens = 0)
        {
            var message = new ChatMessage(
                sessionId: Id,
                role: role,
                content: content,
                status: status,
                payload: payload,
                promptTokens: promptTokens,
                completionTokens: completionTokens
            );

            _messages.Add(message);
            TotalTokensUsed += promptTokens + completionTokens;
            UpdatedAt = DateTime.UtcNow;

            return message;
        }

        // Cập nhật mức sử dụng token nếu tính toán ở bước hoàn tất tin nhắn
        public void AddTokenUsage(int promptTokens, int completionTokens)
        {
            TotalTokensUsed += promptTokens + completionTokens;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}