using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public void Add(Role role)
    {
        _context.Roles.Add(role);
    }
    public async Task<bool> IsNameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var normalized = Role.NormalizeName(name);
        var query = _context.Roles.AsQueryable();

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }

        // Chỉ cần so NormalizedName: đây chính là định nghĩa "trùng" đúng nghĩa (bỏ dấu, bỏ hoa/thường,
        // chuẩn hóa khoảng trắng) - không cần thêm điều kiện so Name thô nữa vì nó chỉ là bản vá tạm
        // cho một logic chuẩn hóa sai trước đó, giờ đã không còn cần thiết.
        return await query.AnyAsync(r => r.NormalizedName == normalized, cancellationToken);
    }
    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
    public async Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
    }

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        if(_context.Entry(role).State == EntityState.Detached)
        {
            _context.Roles.Update(role);
        }
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
    }

    public void Delete(Role role)
    {
        _context.Roles.Remove(role);
    }
    public async Task<bool> IsRoleInUseAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        // Kiểm tra trong bảng trung gian UserRole
        return await _context.Set<UserRole>()
            .AnyAsync(ur => ur.RoleId == roleId, cancellationToken);
    }

    public async Task<(IEnumerable<RoleAdminDto> Roles, int TotalCount)> GetPaginatedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(search)
                                  || r.NormalizedName.ToLower().Contains(search)
                                  || (r.Description != null && r.Description.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderBy(r => r.Name)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            // r.RolePermissions.Count được EF Core dịch thành subquery COUNT trong SQL,
            // KHÔNG tải toàn bộ dòng RolePermissions về bộ nhớ - khác hẳn Include().
            .Select(r => new RoleAdminDto(
                r.Id,
                r.Name,
                r.NormalizedName,
                r.Description,
                r.RolePermissions.Count,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return (roles, totalCount);
    }
}