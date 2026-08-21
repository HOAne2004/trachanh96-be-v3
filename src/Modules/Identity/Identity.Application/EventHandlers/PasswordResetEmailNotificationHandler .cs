using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Identity.Application.EventHandlers;

public class PasswordResetEmailNotificationHandler : INotificationHandler<PasswordResetRequestedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetEmailNotificationHandler> _logger;

    public PasswordResetEmailNotificationHandler(IEmailService emailService, ILogger<PasswordResetEmailNotificationHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PasswordResetRequestedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendResetPasswordEmailAsync(notification.Email, notification.FullName, notification.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email đặt lại mật khẩu thất bại cho UserId: {UserId}", notification.UserId);
        }
    }
}