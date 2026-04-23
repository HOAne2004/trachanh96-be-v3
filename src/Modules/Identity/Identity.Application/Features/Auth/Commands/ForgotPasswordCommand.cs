using FluentValidation;
using Identity.Application.Interfaces;
using Shared.Application.Interfaces;
using MediatR;
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
        private readonly IEmailService _emailService; 

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _emailService = emailService;
        }

        public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                return Result<string>.Success("Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.");
            }

            // 1. Sinh mã OTP
            var resetToken = user.GeneratePasswordResetToken(expiryMinutes: 15);

            // 2. Lưu vào DB thông qua UnitOfWork
            await _userRepository.UpdateAsync(user, cancellationToken);

            // 3. Gửi email thật (Nhớ dùng .Value vì Email giờ là Value Object)
            await _emailService.SendResetPasswordEmailAsync(user.Email.Value, user.FullName, resetToken);

            return Result<string>.Success("Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.");
        }
    }
}