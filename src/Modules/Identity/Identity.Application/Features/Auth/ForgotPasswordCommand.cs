using FluentValidation;
using Identity.Application.Interfaces;
using Shared.Application.Interfaces;
using MediatR;

namespace Identity.Application.Features.Auth
{
    public record ForgotPasswordCommand(string Email) : IRequest<string>;

    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không hợp lệ.");
        }
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService; // Tiêm thêm EmailService

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository,
            IIdentityUnitOfWork unitOfWork,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                return "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
            }

            // 1. Sinh mã OTP
            var resetToken = user.GeneratePasswordResetToken(expiryMinutes: 15);

            // 2. Lưu vào DB thông qua UnitOfWork
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. Gửi email thật (Nhớ dùng .Value vì Email giờ là Value Object)
            await _emailService.SendResetPasswordEmailAsync(user.Email.Value, user.FullName, resetToken);

            return "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
        }
    }
}