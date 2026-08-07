using Identity.Application.DTOs.Request;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Queries
{
    public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDetailsAdminDto>>;

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailsAdminDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public GetUserByIdQueryHandler(IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<Result<UserDetailsAdminDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                return Result<UserDetailsAdminDto>.Failure("Không tìm thấy thông tin người dùng.");
            }

            // Lấy tên các Quyền (Roles)
            var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
            var roles = await _roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);
            var roleNames = roles.Select(r => r.Name).ToList();

            var dto = new UserDetailsAdminDto(
                Id: user.Id,
                Email: user.Email.Value,
                FullName: user.FullName,
                Phone: user.Phone?.Value,
                ThumbnailUrl: user.ThumbnailUrl,
                Roles: roleNames,
                Status: user.Status.ToString(),
                EmailVerified: user.EmailVerified,
                FailedLoginAttempts: user.FailedLoginAttempts,
                LockoutEnd: user.LockoutEnd,
                CreatedAt: user.CreatedAt
            );

            return Result<UserDetailsAdminDto>.Success(dto);
        }
    }
}
