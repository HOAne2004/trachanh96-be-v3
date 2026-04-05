namespace Shared.Domain.Interfaces
{
    public interface ISoftDeletableEntity
    {
        bool IsDeleted { get; set; } 
        DateTime? DeletedAt { get; set; }
    }
}
