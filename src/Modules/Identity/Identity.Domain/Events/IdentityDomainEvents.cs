using Shared.Domain.Interfaces;

namespace Identity.Domain.Events;

public sealed record UserEmailChangedEvent(Guid UserId, string OldEmail, string NewEmail) : DomainEvent;

public sealed record UserAccountLockedEvent(Guid UserId, DateTime LockoutEnd) : DomainEvent;

public sealed record UserAccountDeletedEvent(Guid UserId) : DomainEvent;

public sealed record UserPasswordResetEvent(Guid UserId) : DomainEvent;

public sealed record RolePermissionsChangedEvent(Guid RoleId) : DomainEvent;
public sealed record UserRolesChangedEvent(Guid UserId, IReadOnlyList<Guid> OldRoleIds, IReadOnlyList<Guid> NewRoleIds) : DomainEvent;

public sealed record UserPasswordChangedEvent(Guid UserId) : DomainEvent;

public sealed record UserCreatedByAdminEvent(Guid UserId, IReadOnlyList<Guid> RoleIds) : DomainEvent;