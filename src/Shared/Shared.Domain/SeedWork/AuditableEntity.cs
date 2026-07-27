using Shared.Domain.Interfaces;

namespace Shared.Domain.SeedWork;

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity, ISoftDeletableEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}