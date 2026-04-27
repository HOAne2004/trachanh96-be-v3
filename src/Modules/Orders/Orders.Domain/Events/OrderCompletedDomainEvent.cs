using Shared.Domain.Interfaces;

namespace Orders.Domain.Events
{
    public record OrderCompletedDomainEvent(
        Guid OrderId,
        string OrderCode,
        Guid? CustomerId) : IDomainEvent;
}
