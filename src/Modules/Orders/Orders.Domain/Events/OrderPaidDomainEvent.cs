using Shared.Domain;

namespace Orders.Domain.Events;

public record OrderPaidDomainEvent(
    Guid OrderId,
    string OrderCode,
    string TransactionId,
    Guid? CustomerId,
    decimal FinalAmount 
) : IDomainEvent;