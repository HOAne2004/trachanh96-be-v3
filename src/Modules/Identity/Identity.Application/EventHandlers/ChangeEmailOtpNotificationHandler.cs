using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Identity.Application.EventHandlers
{
    public class ChangeEmailOtpNotificationHandler : INotificationHandler<ChangeEmailRequestedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ChangeEmailOtpNotificationHandler> _logger;

        public ChangeEmailOtpNotificationHandler(IEmailService emailService, ILogger<ChangeEmailOtpNotificationHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(ChangeEmailRequestedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _emailService.SendChangeEmailOtpAsync(notification.PendingEmail, notification.FullName, notification.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi OTP đổi email thất bại cho UserId: {UserId}", notification.UserId);
            }
        }
    }
}
