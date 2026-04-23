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
    public record UpdateProfileCommand(
        Guid UserPublicId, // Lấy từ Token để đảm bảo chỉ sửa profile của chính mình
        string FullName,
        string? PhoneNumber,
        string? ThumbnailUrl
    ) : ICommand<Result<string>>;

    // ==========================================================
    // 2. THE VALIDATOR
    // ==========================================================
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator()
        {
            RuleFor(x => x.UserPublicId).NotEmpty();

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống.")
                .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

            // Chỉ validate format nếu người dùng có nhập số điện thoại
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$").WithMessage("Số điện thoại không đúng định dạng VN.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }
    }

    // ==========================================================
    // 3. THE HANDLER
    // ==========================================================
    public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public UpdateProfileCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
            {
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");
            }

            try
            {
                // Ủy quyền cho Domain xử lý
                user.UpdateProfile(request.FullName, request.PhoneNumber, request.ThumbnailUrl);

                await _userRepository.UpdateAsync(user, cancellationToken);

                // TransactionBehavior sẽ tự động lo việc SaveChanges
                return Result<string>.Success("Cập nhật thông tin cá nhân thành công.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}
