using Payments.Domain.Entities;

namespace Payments.Application.Interfaces
{
    public interface IPaymentRepository
    {
        void Add(PaymentTransaction transaction);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken);
    }
}
