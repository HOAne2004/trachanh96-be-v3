using FluentValidation;
using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using Identity.Application.Models;
using Identity.Domain.Enums;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string DeviceName,
    string IpAddress
) : IRequest<Result<LoginResponseDto>>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token không được để trống.");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork,
        IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<LoginResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hashedOldToken = _jwtProvider.HashToken(request.RefreshToken);

        var user = await _userRepository.GetByRefreshTokenHashAsync(hashedOldToken, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            return Result<LoginResponseDto>.Failure("Refresh token không hợp lệ, đã hết hạn hoặc tài khoản không tồn tại.");
        }

        if (user.Status == UserStatusEnum.Locked && user.LockoutEnd > DateTime.UtcNow)
        {
            return Result<LoginResponseDto>.Failure("Tài khoản đang bị khóa.");
        }

        try
        {
            user.RevokeSession(hashedOldToken);
        }
        catch (DomainException)
        {
            return Result<LoginResponseDto>.Failure("Refresh token đã hết hạn hoặc không hợp lệ.");
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);
        var roleNames = roles.Select(r => r.NormalizedName).ToList();

        // ĐẢO THỨ TỰ: tạo Session mới TRƯỚC để lấy SessionId, sau đó mới sinh Access Token mới
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();
        var newRefreshTokenExpiry = _jwtProvider.GetRefreshTokenExpiry();
        var newHashedToken = _jwtProvider.HashToken(newRefreshToken);

        var newSession = user.AddSession(
            newHashedToken,
            newRefreshTokenExpiry,
            request.DeviceName ?? "Unknown Device",
            request.IpAddress ?? "Unknown IP"
        );

        var tokenUser = new TokenUser(user.Id, user.Email.Value, user.FullName, roleNames, user.SecurityStamp, newSession.Id);
        var newAccessToken = _jwtProvider.GenerateAccessToken(tokenUser);
        var newAccessTokenExpiry = _jwtProvider.GetAccessTokenExpiry();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponseDto>.Success(new LoginResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            AccessTokenExpiry: newAccessTokenExpiry,
            RefreshTokenExpiry: newRefreshTokenExpiry,
            UserId: user.Id,
            Email: user.Email.Value,
            FullName: user.FullName,
            Roles: roleNames,
            ThumbnailUrl: user.ThumbnailUrl
        ));
    }
}