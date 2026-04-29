using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands;

// 1. Command
public record LogoutCommand(
    Guid UserPublicId
) : IRequest<Result<bool>>;

// 2. Validator
public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.UserPublicId)
            .NotEmpty().WithMessage("UserPublicId không được để trống");
    }
}

// 3. Handler
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;

    public LogoutCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm user theo PublicId
        var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("Không tìm thấy người dùng");
        }

        // 2. Xóa Refresh Token khỏi User entity
        user.RevokeRefreshToken();

        // 3. Lưu thay đổi
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 4. Trả về kết quả thành công
        return Result<bool>.Success(true);
    }
}