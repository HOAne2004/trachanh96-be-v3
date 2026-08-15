namespace Shared.Domain.Interfaces;

/// <summary>
/// Marker phân biệt Domain Event dùng làm HỢP ĐỒNG GIỮA CÁC MODULE (Integration Event)
/// khỏi Domain Event nội bộ chỉ dùng trong phạm vi 1 Module (VD: UserEmailChangedEvent chỉ
/// Identity quan tâm). Integration Event đặt tại Shared để mọi Module đều được phép biết,
/// nhưng NGƯỢC LẠI: bản thân nó không được chứa bất kỳ kiểu dữ liệu nào riêng của 1 Module cụ thể
/// (chỉ Guid/string/DateTime/số nguyên thủy) - nếu không, Module nhận sự kiện sẽ phải tham chiếu
/// ngược vào Module gửi, phá vỡ đúng tính độc lập mà Modular Monolith cần giữ.
/// </summary>
public interface IIntegrationEvent : IDomainEvent
{
}

/// <summary>Kế thừa DomainEvent để tái dùng OccurredOn/EventId có sẵn, chỉ thêm ý nghĩa "hợp đồng công khai".</summary>
public abstract record IntegrationEvent : DomainEvent, IIntegrationEvent
{
}