using FluentValidation;
using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using Identity.Application.Models;
using Identity.Domain.Enums;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string? DeviceName = null,
    string? IpAddress = null
) : IRequest<Result<LoginResponseDto>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtProvider jwtProvider,
            IRoleRepository roleRepository,
            IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // BẢO MẬT: Xác thực mật khẩu TRƯỚC khi tiết lộ bất kỳ trạng thái tài khoản nào
        // (IsDeleted/Locked). Nếu kiểm tra trạng thái trước khi verify mật khẩu, kẻ tấn công
        // có thể dò trạng thái tài khoản chỉ bằng cách gửi MẬT KHẨU SAI - vi phạm nguyên tắc
        // chống User Enumeration đã áp dụng nhất quán ở ForgotPasswordCommand.
        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Chỉ tăng đếm sai nếu User thực sự tồn tại và chưa bị xóa mềm - tránh làm bẩn
            // FailedLoginAttempts/Status của một tài khoản đã xóa.
            if (user != null && !user.IsDeleted)
            {
                user.IncreaseFailedLogin();
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<LoginResponseDto>.Failure("Email hoặc mật khẩu không đúng.");
        }

        // Mật khẩu đã đúng - từ đây an toàn để tiết lộ trạng thái tài khoản cụ thể,
        // vì người gọi đã chứng minh được quyền sở hữu thông tin đăng nhập.
        if (user.IsDeleted)
            return Result<LoginResponseDto>.Failure("Tài khoản đã bị xóa.");

        if (user.Status == UserStatusEnum.Locked)
        {
            if (user.LockoutEnd > DateTime.UtcNow)
            {
                return Result<LoginResponseDto>.Failure($"Tài khoản đang bị khóa đến {user.LockoutEnd:dd/MM/yyyy HH:mm}");
            }
            user.UnlockAccount(); // Hết hạn khóa -> Tự mở
        }

        user.ResetFailedLogin(); // Đăng nhập thành công -> Reset đếm sai

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);
        var roleNames = roles.Select(r => r.NormalizedName).ToList();

        var tokenUser = new TokenUser(user.Id, user.Email.Value, user.FullName, roleNames, user.SecurityStamp);

        var accessToken = _jwtProvider.GenerateAccessToken(tokenUser);
        var refreshToken = _jwtProvider.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtProvider.GetRefreshTokenExpiry();
        var accessTokenExpiry = _jwtProvider.GetAccessTokenExpiry(); // Lấy đúng từ 1 nguồn duy nhất, không hardcode lại

        var refreshTokenHash = _jwtProvider.HashToken(refreshToken);

        user.AddSession(
            refreshTokenHash,
            refreshTokenExpiry,
            request.DeviceName ?? "Unknown Device",
            request.IpAddress ?? "Unknown IP"
        );

        await _userRepository.UpdateAsync(user, cancellationToken);
        // Bắt buộc: nếu không, ResetFailedLogin/AddSession sẽ KHÔNG được lưu xuống DB
        // (xem VẤN ĐỀ 0 - cần xác nhận IdentityTransactionBehavior có tự save cho IRequest<T> hay không).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponseDto>.Success(new LoginResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiry: accessTokenExpiry,
            RefreshTokenExpiry: refreshTokenExpiry,
            UserId: user.Id,
            Email: user.Email.Value,
            FullName: user.FullName,
            Roles: roleNames,
            ThumbnailUrl: user.ThumbnailUrl
        ));
    }
}