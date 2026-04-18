using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Commands
{
    public record VerifyEmailCommand(Guid UserPublicId, string OtpToken) : IRequest<Result<string>>;

    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(x => x.UserPublicId).NotEmpty();
            RuleFor(x => x.OtpToken).NotEmpty().WithMessage("Mã xác thực không được để trống.");
        }
    }

    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public VerifyEmailCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");

            try
            {
                // Gọi logic từ Domain: Kiểm tra OTP và chuyển EmailVerified = true
                user.VerifyEmail(request.OtpToken);

                await _userRepository.UpdateAsync(user, cancellationToken);

                return Result<string>.Success("Xác thực email thành công!");
            }
            catch (InvalidOperationException ex)
            {
                // Bắt các lỗi throw từ Domain (VD: Mã sai, mã hết hạn)
                return Result<string>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure("Có lỗi xảy ra trong quá trình xác thực.");
            }
        }
    }
}