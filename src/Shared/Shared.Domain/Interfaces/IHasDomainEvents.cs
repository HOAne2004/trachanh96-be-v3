namespace Shared.Domain.Interfaces;

/// <summary>
/// Interface non-generic để EF Core Interceptor có thể lọc mọi Entity có Domain Event
/// thông qua ChangeTracker.Entries&lt;T&gt;(), bất kể TId của Entity là gì (Guid, string...).
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}