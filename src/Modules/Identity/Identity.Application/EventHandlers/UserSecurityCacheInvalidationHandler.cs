using Identity.Domain.Events;
using MediatR;
using Shared.Application.Interfaces;

namespace Identity.Application.EventHandlers;

/// <summary>
/// Bất kỳ sự kiện nào làm thay đổi SecurityStamp ở tầng Domain đều cần xoá cache tương ứng,
/// nếu không request tiếp theo vẫn so khớp với giá trị SecurityStamp cũ đã cache (tối đa 15 phút trễ).
/// Được publish qua Outbox -> MediatR (xem ProcessOutboxMessagesJob), độ trễ tối đa ~10s (chu kỳ poll).
/// </summary>
public class UserSecurityCacheInvalidationHandler :
    INotificationHandler<UserAccountLockedEvent>,
    INotificationHandler<UserPasswordResetEvent>,
    INotificationHandler<UserAccountDeletedEvent>,
    INotificationHandler<UserEmailChangedEvent>
{
    private readonly ISecurityCacheService _securityCacheService;

    public UserSecurityCacheInvalidationHandler(ISecurityCacheService securityCacheService)
    {
        _securityCacheService = securityCacheService;
    }

    public Task Handle(UserAccountLockedEvent notification, CancellationToken cancellationToken)
        => _securityCacheService.RemoveSecurityStampAsync(notification.UserId);

    public Task Handle(UserPasswordResetEvent notification, CancellationToken cancellationToken)
        => _securityCacheService.RemoveSecurityStampAsync(notification.UserId);

    public Task Handle(UserAccountDeletedEvent notification, CancellationToken cancellationToken)
        => _securityCacheService.RemoveSecurityStampAsync(notification.UserId);

    public Task Handle(UserEmailChangedEvent notification, CancellationToken cancellationToken)
        => _securityCacheService.RemoveSecurityStampAsync(notification.UserId);
}