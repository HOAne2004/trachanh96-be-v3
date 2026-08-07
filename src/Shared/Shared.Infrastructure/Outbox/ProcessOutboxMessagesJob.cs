/// <summary>
/// [BACKGROUND JOB DÙNG CHUNG: OUTBOX PROCESSOR]
/// Đọc OutboxMessage chưa xử lý của một DbContext bất kỳ, publish qua MediatR, đánh dấu đã xử lý.
/// Mỗi Module chỉ cần đăng ký: services.AddHostedService&lt;ProcessOutboxMessagesJob&lt;TDbContext&gt;&gt;();
///
/// LƯU Ý HÀNH VI (khác với bản gốc từng dùng riêng ở Orders):
/// - Bỏ qua các message đã từng lỗi (Error != null) trong các lượt poll sau, để tránh 1 message lỗi
///   vĩnh viễn chặn đứng toàn bộ message mới hơn phía sau nó (Poison Message chặn hàng đợi).
/// - Đây là bản vá tạm (không tự động retry có giới hạn số lần) - message lỗi cần được xử lý thủ công.
///   Nếu muốn retry có kiểm soát (VD: tối đa 3 lần), cần thêm cột RetryCount vào OutboxMessage,
///   kéo theo migration ở CẢ BA module (Orders, Payments, Identity) - cần xác nhận riêng trước khi làm.
/// </summary>

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Domain.Interfaces;

namespace Shared.Infrastructure.Outbox;

public class ProcessOutboxMessagesJob<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly ILogger<ProcessOutboxMessagesJob<TDbContext>> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 20;

    public ProcessOutboxMessagesJob(
        ILogger<ProcessOutboxMessagesJob<TDbContext>> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Outbox Worker: {DbContext}] Bắt đầu khởi động.", typeof(TDbContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Outbox Worker: {DbContext}] Lỗi nghiêm trọng khi quét Outbox.", typeof(TDbContext).Name);
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null && m.Error == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                var domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    message.Content,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                if (domainEvent is null)
                {
                    _logger.LogWarning("[Outbox Worker: {DbContext}] Không thể deserialize message ID: {MessageId}",
                        typeof(TDbContext).Name, message.Id);
                    message.Error = "Deserialize thất bại: kết quả null.";
                    continue;
                }

                await publisher.Publish(domainEvent, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Outbox Worker: {DbContext}] Lỗi khi xử lý message ID: {MessageId}",
                    typeof(TDbContext).Name, message.Id);
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}