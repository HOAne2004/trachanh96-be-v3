using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Identity.Application.EventHandlers
{
    public class AccountInvitationEmailNotificationHandler : INotificationHandler<AccountInvitationRequestedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountInvitationEmailNotificationHandler> _logger;

        public AccountInvitationEmailNotificationHandler(IEmailService emailService, ILogger<AccountInvitationEmailNotificationHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(AccountInvitationRequestedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _emailService.SendWelcomeSetPasswordEmailAsync(notification.Email, notification.FullName, notification.InvitationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi email mời thiết lập mật khẩu thất bại cho UserId: {UserId}", notification.UserId);
            }
        }
    }
}
