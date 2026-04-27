using Shared.Domain.Interfaces;

namespace Orders.Domain.Events
{
    public record OrderShippedDomainEvent(Guid OrderId, string OrderCode, Guid? CustomerId) : IDomainEvent;
}
