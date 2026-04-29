using AI.Application.DTOs;

namespace AI.Application.Interfaces
{
    /// <summary>
    /// Contract giao tiếp với các dịch vụ AI (Gemini, OpenAI, v.v.)
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Gửi tin nhắn của người dùng tới AI và nhận về phản hồi hoặc yêu cầu gọi hàm.
        /// </summary>
        /// <param name="sessionId">ID của phiên chat để AI nhớ ngữ cảnh cuộc trò chuyện.</param>
        /// <param name="userMessage">Tin nhắn của khách hàng (VD: "Cho mình 1 trà đào").</param>
        /// <param name="systemContext">Ngữ cảnh hệ thống bơm vào (VD: Menu hiện tại, thông tin quán).</param>
        /// <returns>Kết quả trả về từ AI bao gồm text hoặc lệnh gọi hàm.</returns>
        Task<AIConversationResult> SendMessageAsync(string sessionId, List<MessageDto> history, string systemContext);
    }
}