using Shared.Domain;

namespace Orders.Domain.Events
{
    public record OrderCheckedOutDomainEvent(
          Guid OrderId,
        string OrderCode,
        decimal FinalTotal,
        string Currency,
        Guid PaymentMethodId
        ) : IDomainEvent;
}
