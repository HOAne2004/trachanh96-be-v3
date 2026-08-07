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

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user != null && !user.IsDeleted)
            {
                user.IncreaseFailedLogin();
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<LoginResponseDto>.Failure("Email hoặc mật khẩu không đúng.");
        }

        if (user.IsDeleted)
            return Result<LoginResponseDto>.Failure("Tài khoản đã bị xóa.");

        if (user.Status == UserStatusEnum.Locked)
        {
            if (user.LockoutEnd > DateTime.UtcNow)
            {
                return Result<LoginResponseDto>.Failure($"Tài khoản đang bị khóa đến {user.LockoutEnd:dd/MM/yyyy HH:mm}");
            }
            user.UnlockAccount();
        }

        user.ResetFailedLogin();

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);
        var roleNames = roles.Select(r => r.NormalizedName).ToList();

        // ĐẢO THỨ TỰ: tạo Refresh Token + Session TRƯỚC, vì UserSession.Id được sinh ngay
        // trong constructor (Guid.CreateVersion7(), không cần chờ SaveChanges) - nên có thể
        // lấy SessionId ngay để nhúng vào Access Token bên dưới.
        var refreshToken = _jwtProvider.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtProvider.GetRefreshTokenExpiry();
        var refreshTokenHash = _jwtProvider.HashToken(refreshToken);

        var session = user.AddSession(
            refreshTokenHash,
            refreshTokenExpiry,
            request.DeviceName ?? "Unknown Device",
            request.IpAddress ?? "Unknown IP"
        );

        var tokenUser = new TokenUser(user.Id, user.Email.Value, user.FullName, roleNames, user.SecurityStamp, session.Id);
        var accessToken = _jwtProvider.GenerateAccessToken(tokenUser);
        var accessTokenExpiry = _jwtProvider.GetAccessTokenExpiry();

        await _userRepository.UpdateAsync(user, cancellationToken);
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