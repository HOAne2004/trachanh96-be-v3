/// <summary>
/// [EF CORE INTERCEPTOR: TỰ ĐỘNG THEO DÕI THỜI GIAN, NGƯỜI THỰC HIỆN & XÓA MỀM]
/// Chức năng: Tự động hóa các thao tác lặp đi lặp lại trước khi dữ liệu được lưu vào Database.
///
/// Cách hoạt động:
/// 1. Auto-Audit: Kiểm tra các Entity implement IAuditableEntity. Tự động gán CreatedAt/CreatedBy
///    khi thêm mới (Added), và LastModifiedAt/LastModifiedBy khi cập nhật (Modified).
/// 2. Soft-Delete (Xóa mềm): Bắt các lệnh xóa (Deleted) đối với Entity implement ISoftDeletableEntity.
///    Ép nó chuyển về trạng thái cập nhật (Modified) và đánh dấu IsDeleted = true, DeletedAt, DeletedBy
///    thay vì xóa vĩnh viễn khỏi DB.
///
/// Nguồn thông tin người thực hiện: ICurrentUser (đọc từ Claim của JWT qua HttpContext).
/// Nếu request không có User đăng nhập hợp lệ (ví dụ: seed data, background job chạy nền),
/// CreatedBy/LastModifiedBy/DeletedBy sẽ là null - Caller (Seeder, Job) cần tự chịu trách nhiệm
/// gán giá trị "System" hoặc tương đương nếu cần audit trail cho tác vụ nền.
///
/// Sử dụng: Đăng ký Interceptor này vào DbContextOptionsBuilder trong hàm cấu hình DbContext của dự án.
/// </summary>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Application.Interfaces;
using Shared.Domain.Interfaces;

namespace Shared.Infrastructure.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;

    public AuditableEntityInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

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

        // Chỉ lấy được UserId khi request có JWT hợp lệ. Với seed data/background job
        // chạy ngoài HTTP context, giá trị này sẽ là Guid.Empty -> lưu null vào cột *By.
        string? currentUserId = _currentUser.IsAuthenticated
            ? _currentUser.UserId.ToString()
            : null;

        // 1. Tự động điền CreatedAt/CreatedBy và LastModifiedAt/LastModifiedBy
        var auditableEntries = dbContext.ChangeTracker.Entries<IAuditableEntity>();
        foreach (var entry in auditableEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.CreatedBy = currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = utcNow;
                entry.Entity.LastModifiedBy = currentUserId;
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
                entry.Entity.DeletedBy = currentUserId;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}