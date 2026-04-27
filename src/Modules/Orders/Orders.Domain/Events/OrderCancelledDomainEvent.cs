using Shared.Domain.Interfaces;

namespace Orders.Domain.Events
{
    public record OrderCancelledDomainEvent(
        Guid OrderId,
        string OrderCode,
        string Reason,
        Guid? CancelledBy) : IDomainEvent;
}
