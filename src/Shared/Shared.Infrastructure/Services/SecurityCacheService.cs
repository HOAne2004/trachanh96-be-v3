using Microsoft.Extensions.Caching.Memory;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Services;

public class SecurityCacheService : ISecurityCacheService
{
    private readonly IMemoryCache _cache;

    public SecurityCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private string GetCacheKey(Guid userId) => $"SecurityStamp_{userId}";
    private string GetRolePermissionsCacheKey(Guid roleId) => $"RolePermissions_{roleId}";

    public Task SetSecurityStampAsync(Guid userId, string stamp, TimeSpan expiration)
    {
        _cache.Set(GetCacheKey(userId), stamp, expiration); // Gọi rất gọn
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(Guid userId)
    {
        var stamp = _cache.TryGetValue(GetCacheKey(userId), out string? cachedStamp) ? cachedStamp : null;
        return Task.FromResult(stamp);
    }

    public Task RemoveSecurityStampAsync(Guid userId)
    {
        _cache.Remove(GetCacheKey(userId));
        return Task.CompletedTask;
    }

    public Task ClearRolePermissionsCacheAsync(Guid roleId)
    {
        _cache.Remove(GetRolePermissionsCacheKey(roleId));
        return Task.CompletedTask;
    }
}