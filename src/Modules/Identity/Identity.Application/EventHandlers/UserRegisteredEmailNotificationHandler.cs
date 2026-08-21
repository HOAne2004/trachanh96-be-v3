using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Identity.Application.EventHandlers;

public class UserRegisteredEmailNotificationHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<UserRegisteredEmailNotificationHandler> _logger;

    public UserRegisteredEmailNotificationHandler(IEmailService emailService, ILogger<UserRegisteredEmailNotificationHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendVerificationEmailAsync(notification.Email, notification.FullName, notification.VerificationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email xác thực đăng ký thất bại cho UserId: {UserId}", notification.UserId);
        }
    }
}