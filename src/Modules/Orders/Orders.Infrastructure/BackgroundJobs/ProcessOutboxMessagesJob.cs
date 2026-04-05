using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orders.Infrastructure.Database;
using Shared.Domain.Interfaces;
using Shared.Infrastructure.Outbox;

namespace Orders.Infrastructure.BackgroundJobs
{
    public class ProcessOutboxMessagesJob : BackgroundService
    {
        private readonly ILogger<ProcessOutboxMessagesJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Tần suất quét: 10 giây 1 lần (Có thể đưa vào appsettings.json trên production)
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);

        public ProcessOutboxMessagesJob(
            ILogger<ProcessOutboxMessagesJob> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[Outbox Worker] Bắt đầu khởi động hệ thống giao báo...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Outbox Worker] Lỗi nghiêm trọng khi quét Outbox.");
                }

                // Nghỉ ngơi trước khi quét lượt tiếp theo
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            // TẠO SCOPE MỚI: Rất quan trọng để tránh Memory Leak
            using var scope = _scopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            // 1. Lấy ra batch 20 message cũ nhất chưa được xử lý
            var messages = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(20) // Limit để không gây tràn RAM nếu queue quá dài
                .ToListAsync(cancellationToken);

            if (!messages.Any()) return; // Không có việc thì về

            foreach (var message in messages)
            {
                try
                {
                    // 2. Phục hồi Event từ chuỗi JSON
                    var domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                        message.Content,
                        new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.All // Bắt buộc để map đúng record class
                        });

                    if (domainEvent is null)
                    {
                        _logger.LogWarning("[Outbox Worker] Không thể deserialize message ID: {MessageId}", message.Id);
                        continue;
                    }

                    // 3. Bắn Event ra toàn hệ thống qua MediatR
                    await publisher.Publish(domainEvent, cancellationToken);

                    // 4. Đánh dấu đã gửi thành công
                    message.ProcessedOnUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Outbox Worker] Lỗi khi xử lý message ID: {MessageId}", message.Id);
                    // Đánh dấu lỗi để không bị kẹt mãi mãi (Có thể tạo luồng Retry sau)
                    message.Error = ex.Message;
                }
            }

            // 5. Lưu trạng thái cập nhật xuống DB
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}