using Identity.Application.DTOs.Response;
using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IRoleRepository
{
    void Add(Role role);
    Task<bool> IsNameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);
    Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task<bool> IsRoleInUseAsync(Guid roleId, CancellationToken cancellationToken = default);
    void Delete(Role role);
    
    // Đổi kiểu trả về: list view chỉ cần đếm số quyền, không cần chi tiết RolePermissions.
    // Khi cần chi tiết (click vào 1 Role), dùng GetByIdWithPermissionsAsync riêng.
    Task<(IEnumerable<RoleAdminDto> Roles, int TotalCount)> GetPaginatedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default);
}