using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

public record AssignRolesToUserCommand(
    Guid TargetUserId,
    List<Guid> RoleIds
) : IRequest<Result<string>>;

public class AssignRolesToUserCommandValidator : AbstractValidator<AssignRolesToUserCommand>
{
    public AssignRolesToUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty().WithMessage("ID người dùng không hợp lệ.");
        RuleFor(x => x.RoleIds)
            .NotNull()
            .NotEmpty().WithMessage("Phải chọn ít nhất 1 quyền cho người dùng.");
    }
}

public class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;
    private readonly ICurrentUser _currentUser;

    public AssignRolesToUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(AssignRolesToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null || user.IsDeleted)
            return Result<string>.Failure("Không tìm thấy người dùng.");

        var newRoleIds = request.RoleIds.Distinct().ToList();
        var validNewRoles = await _roleRepository.GetRolesByIdsAsync(newRoleIds, cancellationToken);
        if (validNewRoles.Count() != newRoleIds.Count)
            return Result<string>.Failure("Có một hoặc nhiều Quyền (Role) không hợp lệ.");

        var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var currentRoles = await _roleRepository.GetRolesByIdsAsync(currentRoleIds, cancellationToken);

        // 1. CHỐNG SELF-LOCKOUT / KHÓA HỆ THỐNG: không cho gỡ Role hệ thống (ADMIN/SUPER_ADMIN)
        // khỏi người dùng CUỐI CÙNG đang giữ Role đó - áp dụng chung cho cả trường hợp Admin
        // tự sửa quyền của mình lẫn sửa quyền người khác, không cần phân biệt 2 trường hợp riêng.
        var protectedRolesBeingRemoved = currentRoles
            .Where(r => !newRoleIds.Contains(r.Id) && ProtectedRoleNames.Names.Contains(r.NormalizedName))
            .ToList();

        foreach (var role in protectedRolesBeingRemoved)
        {
            var remainingHolders = await _userRepository.CountUsersInRoleAsync(role.Id, excludeUserId: user.Id, cancellationToken);
            if (remainingHolders == 0)
            {
                return Result<string>.Failure(
                    $"Không thể gỡ vai trò hệ thống '{role.Name}' khỏi người dùng này vì đây là người dùng cuối cùng đang giữ vai trò này.");
            }
        }

        // 2. CHỐNG LEO THANG ĐẶC QUYỀN: chỉ người đang giữ Role hệ thống mới được gán
        // Role hệ thống cho người khác.
        var protectedRolesBeingAdded = validNewRoles
            .Where(r => !currentRoleIds.Contains(r.Id) && ProtectedRoleNames.Names.Contains(r.NormalizedName))
            .ToList();

        if (protectedRolesBeingAdded.Any() && !_currentUser.Roles.Any(r => ProtectedRoleNames.Names.Contains(r)))
        {
            return Result<string>.Failure("Bạn không có quyền gán vai trò hệ thống cấp cao cho người dùng khác.");
        }

        try
        {
            // SyncRoles (Domain) giờ tự raise UserRolesChangedEvent nếu có thay đổi thực sự - xem mục 3
            user.SyncRoles(newRoleIds);

            user.RevokeAllSessions();
            user.UpdateSecurityStamp();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            return Result<string>.Success("Đã cập nhật quyền thành công. Các phiên làm việc của người dùng này đã bị ngắt để áp dụng quyền mới.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}