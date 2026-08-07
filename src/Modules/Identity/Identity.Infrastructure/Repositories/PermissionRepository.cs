using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly IdentityDbContext _context;

    public PermissionRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public void Add(Permission permission)
    {
        _context.Permissions.Add(permission);
    }

    public async Task<Permission?> GetByIdAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.FirstOrDefaultAsync(p => p.Id == code, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.AnyAsync(p => p.Id == code, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => codes.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Permission> Permissions, int TotalCount)> GetPaginatedAsync(
        int pageIndex, int pageSize, string? searchTerm, string? module, CancellationToken cancellationToken = default)
    {
        var query = _context.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search)
                                  || p.Id.ToLower().Contains(search)
                                  || (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(p => p.Module == module);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var permissions = await query
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (permissions, totalCount);
    }

    public Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        if(_context.Entry(permission).State == EntityState.Detached)
        {
            _context.Permissions.Attach(permission);
        } 
        return Task.CompletedTask;
    }

    public void Delete(Permission permission)
    {
        _context.Permissions.Remove(permission);
    }
}