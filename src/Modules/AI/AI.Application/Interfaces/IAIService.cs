using AI.Application.DTOs;

namespace AI.Application.Interfaces
{
    public interface IAIService
    {
        /// <summary>
        /// Gửi lịch sử đàm thoại và ngữ cảnh hệ thống (Menu/System Context) lên Gemini API.
        /// Trả về phản hồi của AI kèm theo thông số Token tiêu thụ, Payload cho Generative UI và trạng thái an toàn.
        /// </summary>
        /// <param name="sessionId">ID của phiên chat (dùng làm tracking/log)</param>
        /// <param name="history">Lịch sử tin nhắn giữa User và Model</param>
        /// <param name="systemContext">Ngữ cảnh hệ thống (dữ liệu Menu/Quán dưới dạng JSON)</param>
        /// <param name="cancellationToken">Token quản lý việc hủy request</param>
        /// <returns>Đối tượng AIConversationResult đóng gói toàn bộ kết quả và chỉ số hệ thống</returns>
        Task<AIConversationResult> SendMessageAsync(
            string sessionId,
            List<MessageDto> history,
            string systemContext,
            CancellationToken cancellationToken = default);
    }
}