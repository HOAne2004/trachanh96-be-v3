using Shared.Domain;

namespace Orders.Domain.Events
{
    // Bắn ra khi có đơn hàng mới (Dùng để Notification Module báo cho Staff, hoặc Inventory trừ kho tạm)
    public record OrderCreatedDomainEvent(
        Guid OrderId,
        string OrderCode,
        Guid StoreId,
        Guid? CustomerId) : IDomainEvent;
}
