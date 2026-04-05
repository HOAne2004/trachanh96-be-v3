using Payments.Domain.Enums;
using Shared.Domain.Interfaces;

namespace Payments.Domain.Events
{
    public record PaymentSucceededDomainEvent(Guid OrderId, string GatewayTransactionId, PaymentMethodEnum PaymentMethod) : IDomainEvent;
    public record PaymentFailedDomainEvent(Guid OrderId, string ErrorMessage) : IDomainEvent;
    public record PaymentExpiredDomainEvent(Guid OrderId) : IDomainEvent;
    public record PaymentRefundedDomainEvent(Guid OrderId, string RefundTransactionId) : IDomainEvent;
}
