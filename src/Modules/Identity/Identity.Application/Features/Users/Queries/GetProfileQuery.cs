using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Queries;

public record GetProfileQuery() : IRequest<Result<UserProfileResponse>>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<UserProfileResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;

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
        if (!_currentUser.IsAuthenticated)
        {
            return Result<UserProfileResponse>.Failure("Bạn chưa đăng nhập.");
        }

        // Dùng bản nhẹ hơn GetByIdAsync - không tải Addresses/Sessions không cần cho Profile.
        var user = await _userRepository.GetProfileByIdAsync(_currentUser.UserId, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            return Result<UserProfileResponse>.Failure("Tài khoản không tồn tại hoặc đã bị khóa.");
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        var responseDto = new UserProfileResponse(
            Id: user.Id,
            Email: user.Email.Value,
            FullName: user.FullName,
            Roles: roleNames,
            Phone: user.Phone?.Value,
            ThumbnailUrl: user.ThumbnailUrl,
            EmailVerified: user.EmailVerified
        );

        return Result<UserProfileResponse>.Success(responseDto);
    }
}