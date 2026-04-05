using Microsoft.EntityFrameworkCore;
using Payments.Domain.Entities;
using Shared.Infrastructure.Outbox;

namespace Payments.Infrastructure.Database;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    // Bảng giao dịch thanh toán
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    // Bảng Outbox cục bộ của Payments
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Thiết lập Schema mặc định cho toàn bộ module này
        modelBuilder.HasDefaultSchema("payments");

        // Tự động quét và áp dụng cấu hình (Bao gồm PaymentTransactionConfiguration đã tạo ở các phiên trước và OutboxMessageConfiguration ở trên)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}