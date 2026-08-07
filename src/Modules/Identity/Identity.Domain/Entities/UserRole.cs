namespace Identity.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    // --- Navigation Properties ---
    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    protected UserRole() { }

    internal UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}