namespace AI.Application.DTOs
{
    public class AIConversationResult
    {
        // 1. Dữ liệu hiển thị (Text hoặc Cấu trúc)
        public string? TextResponse { get; set; }
        public string? Payload { get; set; } // Nếu AI trả về danh sách sản phẩm dạng JSON cho UI

        // 2. Định tuyến (Có cần gọi Function hay không?)
        public bool RequiresAction { get; set; }
        public string? ActionName { get; set; }
        public string? ActionArguments { get; set; } // JSON arguments từ Function Call (ví dụ: Items của giỏ hàng)

        // 3. Thông số hệ thống (Đo lường & Báo lỗi)
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }

        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? BlockReason { get; set; } // Lý do nếu bị bộ lọc an toàn chặn
    }
}