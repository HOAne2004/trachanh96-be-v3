using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Commands
{
    // ==========================================================
    // 1. THE COMMAND
    // ==========================================================
    public record ChangePasswordCommand(
        Guid UserPublicId, // Lấy từ Token
        string CurrentPassword,
        string NewPassword
    ) : ICommand<Result<string>>;

    // ==========================================================
    // 2. THE VALIDATOR
    // ==========================================================
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserPublicId).NotEmpty();

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu mới phải dài ít nhất 6 ký tự.")
                .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }
    }

    // ==========================================================
    // 3. THE HANDLER
    // ==========================================================
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
            {
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");
            }

            // 1. Kiểm tra xem mật khẩu hiện tại có đúng không
            var isCurrentPasswordValid = _passwordHasher.Verify(user.PasswordHash, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                return Result<string>.Failure("Mật khẩu hiện tại không chính xác.");
            }

            try
            {
                // 2. Băm mật khẩu mới
                var newHashedPassword = _passwordHasher.Hash(request.NewPassword);

                // 3. Gọi logic Domain
                user.ChangePassword(newHashedPassword);

                await _userRepository.UpdateAsync(user, cancellationToken);

                return Result<string>.Success("Đổi mật khẩu thành công.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}
