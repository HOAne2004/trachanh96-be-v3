/// <summary>
/// [EF CORE INTERCEPTOR: PHÁT HÀNH DOMAIN EVENT]
/// Chức năng: Tự động publish toàn bộ Domain Event đã được các Entity/Aggregate Root
/// tích lũy qua AddDomainEvent(), ngay sau khi SaveChangesAsync commit thành công.
///
/// Cách hoạt động:
/// 1. Sau khi DB ghi thành công (SavedChangesAsync), quét ChangeTracker tìm mọi Entity
///    implement IHasDomainEvents có DomainEvents khác rỗng.
/// 2. Publish từng Domain Event qua MediatR (IPublisher).
/// 3. Xóa danh sách event khỏi Entity (ClearDomainEvents) để tránh publish trùng lặp
///    nếu cùng một DbContext gọi SaveChangesAsync nhiều lần trong vòng đời request.
///
/// Lưu ý: publish sau khi commit (không phải trước) để tránh side-effect (gửi email,
/// invalidate cache...) chạy trên dữ liệu chưa chắc được lưu nếu SaveChanges thất bại.
/// </summary>

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Domain.Interfaces;

namespace Shared.Infrastructure.Interceptors;

public class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public DomainEventDispatchInterceptor(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null) return;

        var entitiesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToList();

        if (entitiesWithEvents.Count == 0) return;

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Xóa ngay để tránh publish trùng nếu SaveChangesAsync được gọi lại trong cùng scope
        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);
    }
}