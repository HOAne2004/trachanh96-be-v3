using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IPermissionRepository
{
    void Add(Permission permission);
    Task<Permission?> GetByIdAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Permission> Permissions, int TotalCount)> GetPaginatedAsync(
        int pageIndex, int pageSize, string? searchTerm, string? module, CancellationToken cancellationToken = default);
    Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default);
    void Delete(Permission permission);
}