using Shared.Domain.Interfaces;

namespace Orders.Domain.Events
{
    // Bắn ra khi Staff bấm xong đơn (Dùng để báo cho Customer đến quầy lấy nước)
    public record OrderReadyDomainEvent(
        Guid OrderId,
        string OrderCode,
        Guid? CustomerId) : IDomainEvent;
}
