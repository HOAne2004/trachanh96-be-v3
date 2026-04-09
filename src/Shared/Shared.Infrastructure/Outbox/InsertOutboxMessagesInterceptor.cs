/// <summary>
/// [EF CORE INTERCEPTOR: OUTBOX PATTERN - BƯỚC GHI NHẬN SỰ KIỆN]
/// Chức năng: Đảm bảo "Tính toàn vẹn dữ liệu cuối cùng" (Eventual Consistency) bằng cách lưu Domain Events vào DB cùng một Transaction với dữ liệu nghiệp vụ.
/// 
/// Cách hoạt động:
/// - Trước khi SaveChanges, nó quét toàn bộ Entity để tìm các Domain Events (VD: OrderCreatedEvent) đang nằm chờ.
/// - Serialize (Chuyển thành chuỗi JSON) các Event này và nhét vào bảng OutboxMessage trong Database.
/// - Xóa sạch Event trên Entity sau khi đã "nhét" xong để tránh xử lý trùng.
/// 
/// Sử dụng: Đây là nửa đầu của Outbox Pattern. Đảm bảo mọi sự kiện nghiệp vụ đều được ghi nhận lại 100% không sợ mất điện giữa chừng.
/// </summary>

using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using Shared.Domain.SeedWork;

namespace Shared.Infrastructure.Outbox;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entities = dbContext.ChangeTracker
            .Entries<Entity<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());

        var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredOnUtc = DateTime.UtcNow,
            Type = domainEvent.GetType().Name,
            Content = JsonConvert.SerializeObject(
                domainEvent,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })
        }).ToList();

        dbContext.Set<OutboxMessage>().AddRange(outboxMessages);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}