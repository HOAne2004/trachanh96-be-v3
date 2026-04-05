using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Outbox;

namespace Payments.Infrastructure.Database.Outbox
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            // QUAN TRỌNG: Đặt vào schema "payments" để không bị đụng hàng với Orders
            builder.ToTable("OutboxMessages", "payments");

            builder.HasKey(x => x.Id);

            // Đánh Index để Background Job quét siêu tốc
            builder.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc });
        }
    }
}
