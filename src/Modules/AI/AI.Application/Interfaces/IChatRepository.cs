using AI.Domain.Entities;

namespace AI.Application.Interfaces
{
    public interface IChatRepository
    {
        // Lấy phiên theo SessionId (dùng chính)
        Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

        // Lấy phiên theo OrderId (nếu cần tra cứu lịch sử chat gắn liền với giỏ hàng)
        Task<ChatSession?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

        // Lưu/Cập nhật phiên chat
        Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken);

        // Thêm dòng này vào dưới các hàm đã có
        Task<List<ChatSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

        // Thêm hàm lưu Outbox
        Task AddOutboxMessageAsync(string eventType, string eventContent, CancellationToken cancellationToken);
    }
}