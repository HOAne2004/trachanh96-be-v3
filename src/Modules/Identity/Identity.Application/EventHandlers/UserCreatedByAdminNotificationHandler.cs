using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.EventHandlers;

/// <summary>
/// Việc gửi email mời đã được xử lý TRỰC TIẾP trong CreateUserByAdminCommandHandler (độ trễ thấp).
/// Handler này chỉ phục vụ audit log, và là điểm mở rộng sẵn cho các Module khác trong tương lai
/// cần biết "1 User mới vừa được Admin tạo" (VD: Module HR tự tạo hồ sơ nhân viên tương ứng)
/// mà không cần Identity Module phụ thuộc ngược lại các Module đó - đúng tinh thần Outbox
/// cho giao tiếp liên-Module trong Modular Monolith.
/// </summary>
public class UserCreatedByAdminNotificationHandler : INotificationHandler<UserCreatedByAdminEvent>
{
    private readonly ILogger<UserCreatedByAdminNotificationHandler> _logger;

    public UserCreatedByAdminNotificationHandler(ILogger<UserCreatedByAdminNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserCreatedByAdminEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Tài khoản {UserId} vừa được Admin tạo với {RoleCount} Role được gán.",
            notification.UserId, notification.RoleIds.Count);
        return Task.CompletedTask;
    }
}