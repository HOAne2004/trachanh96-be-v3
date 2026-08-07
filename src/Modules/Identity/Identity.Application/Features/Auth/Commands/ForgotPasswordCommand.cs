using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using Shared.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands
{
    public record ForgotPasswordCommand(string Email) : ICommand<Result<string>>;

    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không hợp lệ.");
        }
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        private const string GenericMessage = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository,
            IIdentityUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<ForgotPasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            // Luôn trả về Success dù có tìm thấy User hay không (Chống User Enumeration)
            if (user == null)
            {
                return Result<string>.Success(GenericMessage);
            }

            // Dùng CSPRNG thay vì Guid.NewGuid() - nhất quán với JwtProvider.GenerateRefreshToken()
            var resetToken = SecureTokenGenerator.GenerateReadableCode(8);

            user.SetPasswordResetToken(resetToken, expiryMinutes: 15);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                await _emailService.SendResetPasswordEmailAsync(user.Email.Value, user.FullName, resetToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi email đặt lại mật khẩu thất bại cho UserId: {UserId}", user.Id);
            }

            return Result<string>.Success(GenericMessage);
        }
    }
}