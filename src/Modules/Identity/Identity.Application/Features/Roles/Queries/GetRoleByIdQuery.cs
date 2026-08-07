using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Roles.Queries;

// ==========================================================
// 1. DTO DETAIL CHO MÀN HÌNH CHỈNH SỬA PHÂN QUYỀN
// ==========================================================
public record RoleDetailsAdminDto(
    Guid Id,
    string Name,
    string NormalizedName,
    string? Description,
    List<string> PermissionCodes, // Danh sách các mã quyền (VD: ["Identity.Users.View", "Catalog.Products.Create"])
    DateTime CreatedAt
);

// ==========================================================
// 2. THE QUERY
// ==========================================================
public record GetRoleByIdQuery(Guid RoleId) : IRequest<Result<RoleDetailsAdminDto>>;

// ==========================================================
// 3. THE VALIDATOR
// ==========================================================
public class GetRoleByIdQueryValidator : AbstractValidator<GetRoleByIdQuery>
{
    public GetRoleByIdQueryValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("ID Vai trò (Role) không được để trống.");
    }
}

// ==========================================================
// 4. THE HANDLER
// ==========================================================
public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDetailsAdminDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<RoleDetailsAdminDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        // Gọi hàm GetByIdWithPermissionsAsync để Include sẵn bảng trung gian RolePermissions
        var role = await _roleRepository.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken);

        if (role == null)
        {
            return Result<RoleDetailsAdminDto>.Failure("Không tìm thấy Vai trò (Role).");
        }

        var dto = new RoleDetailsAdminDto(
            Id: role.Id,
            Name: role.Name,
            NormalizedName: role.NormalizedName,
            Description: role.Description,
            PermissionCodes: role.RolePermissions.Select(rp => rp.PermissionId).ToList(),
            CreatedAt: role.CreatedAt
        );

        return Result<RoleDetailsAdminDto>.Success(dto);
    }
}