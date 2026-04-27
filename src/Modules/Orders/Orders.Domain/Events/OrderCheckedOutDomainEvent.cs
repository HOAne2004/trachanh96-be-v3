using Shared.Domain.Interfaces;

namespace Orders.Domain.Events
{
    public record OrderCheckedOutDomainEvent(
          Guid OrderId,
        string OrderCode,
        decimal FinalAmount,
        string Currency,
        Guid PaymentMethodId
        ) : IDomainEvent;
}
