using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Application.Interfaces;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Auth.Commands;

public record LogoutCommand(
    string RefreshToken
) : IRequest<Result<bool>>;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken không được để trống");
    }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        IJwtProvider jwtProvider,
        ICurrentUser currentUser,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<bool>.Failure("Bạn chưa đăng nhập.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("Không tìm thấy người dùng.");
        }

        var hashedToken = _jwtProvider.HashToken(request.RefreshToken);

        try
        {
            user.RevokeSession(hashedToken);
        }
        catch (DomainException)
        {
            // Chỉ bắt riêng DomainException ("Không tìm thấy phiên đăng nhập.") - coi như đã
            // đăng xuất, không cần báo lỗi. Các Exception khác (lỗi hệ thống thật) sẽ propagate
            // ra ngoài thay vì bị nuốt thành "Success" giả.
            return Result<bool>.Success(true);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // Bắt buộc: lưu trạng thái Revoke

        return Result<bool>.Success(true);
    }
}