namespace AI.Application.DTOs
{
    public class AIConversationResult
    {
        /// <summary>
        /// Câu trả lời bằng văn bản của AI để hiển thị lên UI cho khách hàng.
        /// VD: "Dạ, bạn muốn size M hay L ạ?" hoặc "Đã thêm vào giỏ hàng cho bạn."
        /// </summary>
        public string? TextResponse { get; set; }

        /// <summary>
        /// Cờ báo hiệu AI có muốn thực hiện một hành động (Function Call) hay không.
        /// </summary>
        public bool RequiresAction { get; set; }

        /// <summary>
        /// Tên của hàm mà AI muốn gọi (nếu RequiresAction = true).
        /// VD: "AddToCart" hoặc "CheckOrderStatus".
        /// </summary>
        public string? ActionName { get; set; }

        /// <summary>
        /// Các tham số AI trích xuất được từ câu nói của khách, định dạng dưới chuỗi JSON.
        /// VD: "{\"productId\": 5, \"quantity\": 1, \"note\": \"ít đá\"}"
        /// </summary>
        public string? ActionArguments { get; set; }
    }
}