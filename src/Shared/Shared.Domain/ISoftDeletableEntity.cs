namespace Shared.Domain
{
    public interface ISoftDeletableEntity
    {
        bool IsDeleted { get; set; } 
        DateTime? DeletedAt { get; set; }
    }
}
