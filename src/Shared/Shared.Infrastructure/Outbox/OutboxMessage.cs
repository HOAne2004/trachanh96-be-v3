/// <summary>
/// [ENTITY: HỘP THƯ ĐI (OUTBOX MESSAGE)]
/// Chức năng: Lưu trữ các Domain Events dưới dạng chuỗi JSON trong Database để chờ được xử lý ở chế độ nền (Background).
/// 
/// Cách hoạt động:
/// - Content: Chứa toàn bộ object Event đã bị JSON hóa.
/// - ProcessedOnUtc: Đánh dấu thời điểm Background Job đã xử lý (VD: Gửi mail) thành công thư này.
/// - Error: Nếu Background Job gửi mail lỗi 3 lần, nó sẽ lưu nguyên nhân lỗi vào đây để IT dễ debug.
/// </summary>

namespace Shared.Infrastructure.Outbox
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // Tên của class Event (VD: OrderCreatedDomainEvent)
        public string Content { get; set; } = string.Empty; // Chuỗi JSON chứa dữ liệu
        public DateTime OccurredOnUtc { get; set; }
        public DateTime? ProcessedOnUtc { get; set; } // Nếu NULL nghĩa là chưa xử lý
        public string? Error { get; set; } // Chứa thông báo lỗi nếu quá trình gửi thất bại
    }
}
