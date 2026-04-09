/// <summary>
/// [EF CORE INTERCEPTOR: TỰ ĐỘNG THEO DÕI THỜI GIAN & XÓA MỀM]
/// Chức năng: Tự động hóa các thao tác lặp đi lặp lại trước khi dữ liệu được lưu vào Database.
/// 
/// Cách hoạt động:
/// 1. Auto-Audit: Kiểm tra các Entity implement IAuditableEntity. Tự động gán CreatedAt khi thêm mới (Added), và UpdatedAt khi cập nhật (Modified).
/// 2. Soft-Delete (Xóa mềm): Bắt các lệnh xóa (Deleted) đối với Entity implement ISoftDeletableEntity. Ép nó chuyển về trạng thái cập nhật (Modified) và đánh dấu IsDeleted = true thay vì xóa vĩnh viễn khỏi DB.
/// 
/// Sử dụng: Đăng ký Interceptor này vào DbContextOptionsBuilder trong hàm cấu hình DbContext của dự án. Không cần viết code cập nhật thời gian thủ công ở các Handler nữa.
/// </summary>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Domain.Interfaces;

namespace Shared.Infrastructure.Interceptors;


public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        DbContext? dbContext = eventData.Context;
        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        DateTime utcNow = DateTime.UtcNow;

        // 1. Tự động điền CreatedAt và UpdatedAt
        var auditableEntries = dbContext.ChangeTracker.Entries<IAuditableEntity>();
        foreach (var entry in auditableEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        // 2. Chặn thao tác xóa cứng (Hard Delete) và chuyển thành xóa mềm (Soft Delete)
        var softDeletableEntries = dbContext.ChangeTracker.Entries<ISoftDeletableEntity>();
        foreach (var entry in softDeletableEntries)
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified; // Ép nó về trạng thái Modified thay vì Deleted
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = utcNow;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}