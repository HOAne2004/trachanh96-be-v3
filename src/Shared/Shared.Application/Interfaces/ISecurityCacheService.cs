namespace Shared.Application.Interfaces;

public interface ISecurityCacheService
{
    Task SetSecurityStampAsync(Guid userId, string stamp, TimeSpan expiration);
    Task<string?> GetSecurityStampAsync(Guid userId);
    Task RemoveSecurityStampAsync(Guid userId);
    Task ClearRolePermissionsCacheAsync(Guid roleId);
}