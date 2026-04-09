/// <summary>
/// [DDD SEEDWORK: AGGREGATE ROOT]
/// Chức năng: Đánh dấu một Entity là Thực thể gốc (Cửa ngõ của một cụm dữ liệu).
/// Quy tắc DDD:
/// - Repository CHỈ được phép làm việc trực tiếp với Aggregate Root (VD: IOrderRepository, không có IOrderItemRepository).
/// - Mọi thao tác thay đổi trạng thái của các Entity con (Child Entities) đều phải đi qua các method của Aggregate Root để đảm bảo tính toàn vẹn dữ liệu (Invariants).
/// Sử dụng: Kế thừa lớp này cho các Entity làm gốc (VD: Order, Product, User).
/// </summary>

namespace Shared.Domain.SeedWork
{
    public abstract class AggregateRoot<TId> : Entity<TId>
    {

    }
}
