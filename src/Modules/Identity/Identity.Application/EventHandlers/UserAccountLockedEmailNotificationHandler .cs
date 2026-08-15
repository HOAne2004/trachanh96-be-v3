using Identity.Application.Interfaces;
using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Identity.Application.EventHandlers;

/// <summary>
/// Điểm gửi email DUY NHẤT cho mọi trường hợp khóa tài khoản, bất kể nguồn: tự động (đăng nhập
/// sai quá số lần), Admin chủ động, hay module khác yêu cầu qua Integration Event. Tránh viết
/// lặp logic gửi email ở 3 nơi khác nhau.
/// </summary>
public class UserAccountLockedEmailNotificationHandler : INotificationHandler<UserAccountLockedEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserAccountLockedEmailNotificationHandler> _logger;

    public UserAccountLockedEmailNotificationHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<UserAccountLockedEmailNotificationHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(UserAccountLockedEvent notification, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user == null) return; // Tài khoản có thể đã bị xóa ngay sau đó - không phải lỗi.

        try
        {
            await _emailService.SendAccountLockedEmailAsync(
                user.Email.Value, user.FullName, notification.Reason, notification.LockoutEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email thông báo khóa tài khoản thất bại cho UserId: {UserId}", notification.UserId);
        }
    }
}