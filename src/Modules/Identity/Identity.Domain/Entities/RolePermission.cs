namespace Identity.Domain.Entities;

public class RolePermission
{
    public Guid RoleId { get; private set; }
    public string PermissionId { get; private set; }

    // --- Navigation Properties ---
    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;

    protected RolePermission() { PermissionId = null!; }

    internal RolePermission(Guid roleId, string permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}