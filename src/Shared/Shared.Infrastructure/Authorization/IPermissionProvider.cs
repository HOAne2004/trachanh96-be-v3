namespace Shared.Application.Authorization;

// DTO chứa thông tin quyền để truyền đi
public record PermissionDefinition(string Code, string Name, string Module, string? Description = null, bool IsSystem = false);

public interface IPermissionProvider
{
    IEnumerable<PermissionDefinition> GetPermissions();
}