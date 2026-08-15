using Shared.Domain.Interfaces;

namespace Shared.Domain.IntegrationEvents;

/// <summary>
/// Bất kỳ Module nào (Orders, Community/Review...) phát hiện hành vi đáng ngờ của 1 User
/// (boom hàng, report vi phạm...) đều raise sự kiện này từ chính Entity của Module đó -
/// KHÔNG gọi trực tiếp vào Identity.Application. Identity sẽ tự xử lý qua Outbox + MediatR.
/// </summary>
public sealed record AccountLockRequestedIntegrationEvent(
    Guid UserId,
    string Reason,
    string SourceModule,
    int LockDurationDays
) : IntegrationEvent;