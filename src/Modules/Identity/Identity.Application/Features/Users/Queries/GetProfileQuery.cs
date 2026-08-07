using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Queries;


// 1. Query: KHÔNG NHẬN THAM SỐ NÀO CẢ (Self-Service)
public record GetProfileQuery() : IRequest<Result<UserProfileResponse>>;

// 2. Handler
public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<UserProfileResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser; // Inject chiếc "chìa khóa" Context

    public GetProfileQueryHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra xác thực ở tầng Application (Double check)
        if (!_currentUser.IsAuthenticated)
        {
            return Result<UserProfileResponse>.Failure("Bạn chưa đăng nhập.");
        }

        // 2. Tự động lấy UserId từ Token, ngăn chặn triệt để tấn công IDOR
        var userId = _currentUser.UserId;

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            return Result<UserProfileResponse>.Failure("Tài khoản không tồn tại hoặc đã bị khóa.");
        }

        // 3. Lấy tên các quyền (Roles) một cách chính xác
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList(); // Dùng Name (hiển thị) hoặc NormalizedName tùy UI

        // 4. Map sang DTO
        var responseDto = new UserProfileResponse(
            Id: user.Id,
            Email: user.Email.Value, // Email giờ là Value Object
            FullName: user.FullName,
            Roles: roleNames,
            Phone: user.Phone?.Value, // Phone giờ là Value Object
            ThumbnailUrl: user.ThumbnailUrl,
            EmailVerified: user.EmailVerified
        );

        return Result<UserProfileResponse>.Success(responseDto);
    }
}