using FluentValidation;
using Identity.Application.DTOs;
using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using Identity.Application.Models;
using Identity.Domain.Enums;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands;

// 1. Command (Cần nhận thêm thông tin thiết bị để lưu Session mới)
public record RefreshTokenCommand(
    string RefreshToken,
    string DeviceName,
    string IpAddress
) : IRequest<Result<LoginResponseDto>>;

// 2. Validator
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token không được để trống.");
    }
}

// 3. Handler
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
        // 1. Băm token do client gửi lên để tìm kiếm trong DB
        var hashedOldToken = _jwtProvider.HashToken(request.RefreshToken);

        // 2. Tìm User chứa Session có mã Hash này
        var user = await _userRepository.GetByRefreshTokenHashAsync(hashedOldToken, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            return Result<LoginResponseDto>.Failure("Refresh token không hợp lệ hoặc tài khoản không tồn tại.");
        }

        // 3. Kiểm tra User có đang bị khóa không
        if (user.Status == UserStatusEnum.Locked && user.LockoutEnd > DateTime.UtcNow)
        {
            return Result<LoginResponseDto>.Failure("Tài khoản đang bị khóa.");
        }

        // 4. Thu hồi Session cũ (Cơ chế Token Rotation)
        try
        {
            user.RevokeSession(hashedOldToken);
        }
        catch (Exception)
        {
            return Result<LoginResponseDto>.Failure("Refresh token đã hết hạn hoặc không hợp lệ.");
        }

        // 5. Chuẩn bị dữ liệu tạo Token mới
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);
        var roleNames = roles.Select(r => r.NormalizedName).ToList();

        var tokenUser = new TokenUser(user.Id, user.Email.Value, user.FullName, roleNames, user.SecurityStamp);

        // 6. Tạo cặp Tokens mới
        var newAccessToken = _jwtProvider.GenerateAccessToken(tokenUser);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();
        var newRefreshTokenExpiry = _jwtProvider.GetRefreshTokenExpiry();
        var newHashedToken = _jwtProvider.HashToken(newRefreshToken);

        // 7. Lưu Session mới
        user.AddSession(
            newHashedToken,
            newRefreshTokenExpiry,
            request.DeviceName ?? "Unknown Device",
            request.IpAddress ?? "Unknown IP"
        );

        // 8. Lưu thay đổi
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 9. Trả kết quả
        return Result<LoginResponseDto>.Success(new LoginResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken, // Trả token gốc cho client
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiry: newRefreshTokenExpiry,
            UserId: user.Id,
            Email: user.Email.Value,
            FullName: user.FullName,
            Roles: roleNames,
            ThumbnailUrl: user.ThumbnailUrl
        ));
    }
}