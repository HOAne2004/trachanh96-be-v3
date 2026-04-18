using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Commands
{
    public record ChangeEmailCommand(Guid UserPublicId, string NewEmail) : IRequest<Result<string>>;

    public class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
    {
        public ChangeEmailCommandValidator()
        {
            RuleFor(x => x.UserPublicId).NotEmpty();
            RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().WithMessage("Email mới không hợp lệ.");
        }
    }

    public class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService; 

        public ChangeEmailCommandHandler(IUserRepository userRepository, IEmailService emailService)
        {
            _userRepository = userRepository;
            _emailService = emailService;
        }

        public async Task<Result<string>> Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
        {
            var emailExists = await _userRepository.IsEmailExistsAsync(request.NewEmail, cancellationToken);
            if (emailExists)
                return Result<string>.Failure("Email này đã được sử dụng bởi một tài khoản khác.");

            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");

            try
            {
                user.ChangeEmail(request.NewEmail);
                var otpToken = user.GenerateEmailVerificationToken();

                await _userRepository.UpdateAsync(user, cancellationToken);

                await _emailService.SendChangeEmailOtpAsync(request.NewEmail, user.FullName, otpToken);

                return Result<string>.Success("Đã gửi mã xác thực đến email mới. Vui lòng kiểm tra hộp thư.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}
