using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Roles.Commands;

public record UpdateRolePermissionsCommand(Guid RoleId, List<string> PermissionCodes) : IRequest<Result<bool>>;

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("ID Vai trò không được để trống.");
        RuleFor(x => x.PermissionCodes).NotNull().WithMessage("Danh sách quyền không hợp lệ.");
    }
}

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;

    public UpdateRolePermissionsCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
    }

    public async Task<Result<bool>> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken);
        if (role == null) return Result<bool>.Failure("Không tìm thấy Vai trò (Role).");

        if (ProtectedRoleNames.Names.Contains(role.NormalizedName))
        {
            return Result<bool>.Failure($"Không thể chỉnh sửa ma trận phân quyền của Vai trò hệ thống '{role.Name}'.");
        }

        var currentPermissionCodes = role.RolePermissions.Select(rp => rp.PermissionId).ToList();
        var permissionsToAdd = request.PermissionCodes.Except(currentPermissionCodes).ToList();
        var permissionsToRemove = currentPermissionCodes.Except(request.PermissionCodes).ToList();

        // Validate TRƯỚC khi thực hiện bất kỳ mutation nào - đảm bảo "tất cả hoặc không gì cả",
        // tránh trường hợp đã lỡ RemovePermission một số quyền rồi mới phát hiện mã Add sai.
        var entitiesToAdd = new List<Permission>();
        if (permissionsToAdd.Any())
        {
            entitiesToAdd = (await _permissionRepository.GetByCodesAsync(permissionsToAdd, cancellationToken)).ToList();
            if (entitiesToAdd.Count != permissionsToAdd.Count)
            {
                return Result<bool>.Failure("Có một hoặc nhiều mã Quyền (Permission) không hợp lệ.");
            }
        }

        try
        {
            foreach (var code in permissionsToRemove)
            {
                role.RemovePermission(code);
            }

            foreach (var permission in entitiesToAdd)
            {
                role.AddPermission(permission);
            }

            await _roleRepository.UpdateAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _securityCacheService.ClearRolePermissionsCacheAsync(role.Id);

            return Result<bool>.Success(true);
        }
        catch (DomainException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
    }
}