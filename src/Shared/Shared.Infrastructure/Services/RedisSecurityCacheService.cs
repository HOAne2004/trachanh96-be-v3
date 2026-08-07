using Microsoft.Extensions.Caching.Distributed;
using Shared.Application.Interfaces;
using System.Text.Json;

namespace Shared.Infrastructure.Services;

public class RedisSecurityCacheService : ISecurityCacheService
{
    private readonly IDistributedCache _cache;

    // Prefix theo module để tránh đụng key với Catalog/Order/Store... khi dùng chung 1 Redis instance
    private const string KeyPrefix = "identity";

    public RedisSecurityCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    private static string GetCacheKey(Guid userId) => $"{KeyPrefix}:security-stamp:{userId}";
    private static string GetRolePermissionsCacheKey(Guid roleId) => $"{KeyPrefix}:role-permissions:{roleId}";

    public Task SetSecurityStampAsync(Guid userId, string stamp, TimeSpan expiration)
    {
        return _cache.SetStringAsync(
            GetCacheKey(userId),
            stamp,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });
    }

    public async Task<string?> GetSecurityStampAsync(Guid userId)
    {
        return await _cache.GetStringAsync(GetCacheKey(userId));
    }

    public Task RemoveSecurityStampAsync(Guid userId)
    {
        return _cache.RemoveAsync(GetCacheKey(userId));
    }

    public async Task<List<string>?> GetRolePermissionsAsync(Guid roleId)
    {
        var json = await _cache.GetStringAsync(GetRolePermissionsCacheKey(roleId));
        return json is null ? null : JsonSerializer.Deserialize<List<string>>(json);
    }

    public Task SetRolePermissionsAsync(Guid roleId, IEnumerable<string> permissionCodes, TimeSpan expiration)
    {
        var json = JsonSerializer.Serialize(permissionCodes.ToList());
        return _cache.SetStringAsync(
            GetRolePermissionsCacheKey(roleId),
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });
    }

    public Task ClearRolePermissionsCacheAsync(Guid roleId)
    {
        return _cache.RemoveAsync(GetRolePermissionsCacheKey(roleId));
    }
}