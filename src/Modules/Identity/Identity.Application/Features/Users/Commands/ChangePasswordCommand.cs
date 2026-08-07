using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND
// ==========================================================
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu mới phải dài ít nhất 6 ký tự.")
            .Matches(@"[A-Z]+").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa.")
            .Matches(@"[0-9]+").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.")
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
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
    }

    public async Task<Result<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra xác thực (Self-service gate)
        if (!_currentUser.IsAuthenticated)
        {
            return Result<string>.Failure("Bạn chưa đăng nhập.");
        }

        // 2. Lấy User từ Token thay vì request
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            return Result<string>.Failure("Không tìm thấy tài khoản người dùng hoặc tài khoản đã bị khóa.");
        }

        // 3. Kiểm tra xem mật khẩu hiện tại có đúng không
        var isCurrentPasswordValid = _passwordHasher.Verify(user.PasswordHash, request.CurrentPassword);
        if (!isCurrentPasswordValid)
        {
            // Tùy chọn: Có thể gọi user.IncreaseFailedLogin() ở đây nếu muốn siết chặt bảo mật
            return Result<string>.Failure("Mật khẩu hiện tại không chính xác.");
        }

        try
        {
            // 4. Băm mật khẩu mới
            var newHashedPassword = _passwordHasher.Hash(request.NewPassword);

            // 5. Cập nhật Entity
            user.ChangePassword(newHashedPassword);

            // BẢO MẬT TỐI CAO: Khóa mõm tất cả các thiết bị đang đăng nhập khác và đổi Stamp!
            user.RevokeAllSessions();
            user.UpdateSecurityStamp();

            // 6. Lưu xuống DB
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 7. Cập nhật Cache để Middleware đánh văng JWT cũ ngay tức khắc
            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            return Result<string>.Success("Đổi mật khẩu thành công. Vui lòng đăng nhập lại trên các thiết bị.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}