/// <summary>
/// [DDD SEEDWORK: BASE ENTITY]
/// Chức năng: Lớp nền tảng cho mọi Thực thể (Entity) trong hệ thống.
/// Đặc điểm:
/// - Tính định danh (Identity): Hai Entity được coi là giống nhau NẾU VÀ CHỈ NẾU chúng có cùng Id, bất kể các thuộc tính khác có khác nhau.
/// - Chứa danh sách Domain Events: Nơi lưu trữ tạm thời các sự kiện nghiệp vụ (VD: OrderCreatedEvent) trước khi được UnitOfWork xuất bản (publish) ra toàn hệ thống.
/// Sử dụng: Kế thừa lớp này cho các đối tượng có vòng đời và cần theo dõi bằng Id (VD: OrderItem, ProductVariant...).
/// </summary>

using Shared.Domain.Interfaces;

namespace Shared.Domain.SeedWork
{
    public abstract class Entity<TId> : IEquatable<Entity<TId>>
    {
        public TId Id { get; protected set; } = default!;

        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Entity<TId> other) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetType() != other.GetType()) return false;
            if (Id is null || Id.Equals(default)) return false;

            return Id.Equals(other.Id);
        }

        public bool Equals(Entity<TId>? other) => Equals((object?)other);

        public override int GetHashCode() => Id?.GetHashCode() ?? 0;
    }
}
