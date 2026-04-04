// lưu trữ các Domain Events (những "sự thật đã xảy ra trong quá khứ") dưới dạng chuỗi JSON.
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
