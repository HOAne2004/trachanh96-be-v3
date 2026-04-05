using Shared.Domain.Interfaces;

namespace Orders.Domain.Events
{
    public record OrderPreparingDomainEvent(
        Guid OrderId,
        string OrderCode) : IDomainEvent;
}
