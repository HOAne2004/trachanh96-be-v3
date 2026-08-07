using Shared.Domain.Interfaces;

namespace Identity.Domain.Events;

public sealed record UserEmailChangedEvent(Guid UserId, string OldEmail, string NewEmail) : DomainEvent;

public sealed record UserAccountLockedEvent(Guid UserId, DateTime LockoutEnd) : DomainEvent;

public sealed record UserAccountDeletedEvent(Guid UserId) : DomainEvent;

public sealed record UserPasswordResetEvent(Guid UserId) : DomainEvent;

public sealed record RolePermissionsChangedEvent(Guid RoleId) : DomainEvent;