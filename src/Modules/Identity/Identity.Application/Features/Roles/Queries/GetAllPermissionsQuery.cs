using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Roles.Queries;

public record PermissionGroupDto(string ModuleName, List<PermissionDto> Permissions);

public record GetAllPermissionsQuery() : IRequest<Result<List<PermissionGroupDto>>>;

public class GetAllPermissionsQueryHandler : IRequestHandler<GetAllPermissionsQuery, Result<List<PermissionGroupDto>>>
{
    private readonly IPermissionRepository _permissionRepository;

    public GetAllPermissionsQueryHandler(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<Result<List<PermissionGroupDto>>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);

        var groupedPermissions = permissions
            .GroupBy(p => p.Module)
            .Select(group => new PermissionGroupDto(
                ModuleName: group.Key,
                Permissions: group.Select(p => new PermissionDto(
                    Code: p.Id,
                    Name: p.Name,
                    Description: p.Description ?? string.Empty
                )).ToList()
            ))
            .OrderBy(g => g.ModuleName)
            .ToList();

        return Result<List<PermissionGroupDto>>.Success(groupedPermissions);
    }
}