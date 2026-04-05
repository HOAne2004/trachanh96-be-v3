using MediatR;
namespace Shared.Domain.Interfaces
{
    public interface IDomainEvent : INotification { }

    //Mở rộng thêm một lớp trừu tượng để chứa những thuộc tính chung của tất cả các sự kiện (nếu cần)
    //public abstract record DomainEvent : IDomainEvent
    //{
    //    // Thời điểm sự kiện xảy ra.
    //    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    //    public Guid EventId { get; init; } = Guid.NewGuid();
    //}
}
