using MediatR;

namespace Shared.Application.IntegrationEvents
{
    public record ReservationIntentDetectedIntegrationEvent(
        Guid SessionId,
        string ReservationDetailsJson,
        DateTime OccurredOn
    ) : INotification;
}