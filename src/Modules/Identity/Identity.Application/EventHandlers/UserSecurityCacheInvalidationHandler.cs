using Identity.Domain.Events;
using MediatR;
using Shared.Application.Interfaces;

namespace Identity.Application.EventHandlers;

public class UserSecurityCacheInvalidationHandler :
    INotificationHandler<UserAccountLockedEvent>,
    INotificationHandler<UserPasswordResetEvent>,
    INotificationHandler<UserPasswordChangedEvent>,
    INotificationHandler<UserAccountDeletedEvent>,
    INotificationHandler<UserEmailChangedEvent>,
    INotificationHandler<UserRolesChangedEvent>
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

    public Task Handle(UserPasswordChangedEvent notification, CancellationToken cancellationToken)
        => _securityCacheService.RemoveSecurityStampAsync(notification.UserId);

    public Task Handle(UserAccountDeletedEvent notification, CancellationToken cancellationToken)
        => _securityCacheService.RemoveSecurityStampAsync(notification.UserId);

    public Task Handle(UserEmailChangedEvent notification, CancellationToken cancellationToken)
        => _securityCacheService.RemoveSecurityStampAsync(notification.UserId);

    // Safety-net: AssignRolesToUserCommandHandler đã write-through cache trực tiếp, tương tự
    // pattern đã áp dụng cho RolePermissionsChangedEvent - handler này là lưới an toàn nếu
    // lệnh gọi trực tiếp thất bại vì lý do hạ tầng.
    public Task Handle(UserRolesChangedEvent notification, CancellationToken cancellationToken)
        => _securityCacheService.RemoveSecurityStampAsync(notification.UserId);
}