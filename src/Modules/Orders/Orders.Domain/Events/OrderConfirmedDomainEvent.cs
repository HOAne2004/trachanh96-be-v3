using Shared.Domain.Interfaces;

namespace Orders.Domain.Events
{
    public record OrderConfirmedDomainEvent(
        Guid OrderId,
        string OrderCode,
        Guid? CustomerId) : IDomainEvent;
}
