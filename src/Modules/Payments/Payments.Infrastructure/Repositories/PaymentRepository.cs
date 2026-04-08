
using Payments.Application.Interfaces;
using Payments.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Payments.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentsDbContext _dbContext;
        public PaymentRepository(PaymentsDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void Add(Domain.Entities.PaymentTransaction transaction)
        {
            // Lưu ý: Do chúng ta đã sử dụng TransactionBehavior để tự động gọi SaveChanges,
            // nên ở đây chúng ta chỉ cần Add vào DbSet, không cần gọi SaveChanges nữa.
            _dbContext.PaymentTransactions.Add(transaction);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<Domain.Entities.PaymentTransaction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken)
        {
            return await _dbContext.PaymentTransactions.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        }
    }
}
