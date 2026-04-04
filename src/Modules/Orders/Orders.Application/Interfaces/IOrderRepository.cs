using Orders.Domain.Entities;

namespace Orders.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByOrderCodeAsync(string OrderCode, CancellationToken cancellationToken = default);
        Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // BỔ SUNG: Dùng cho các tác vụ nhạy cảm về tiền bạc (Webhook, Checkout)
        Task<Order?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default);
        void Add(Order order);
    }
}
