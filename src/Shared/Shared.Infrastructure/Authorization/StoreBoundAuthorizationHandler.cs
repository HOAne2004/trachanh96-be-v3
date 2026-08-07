using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Shared.Application.Authorization;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Authorization;

// Handler này sẽ tự động chạy khi resource truyền vào có implement IStoreBoundEntity
public class StoreBoundAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, IStoreBoundEntity>
{
    private readonly ICurrentUser _currentUser;

    public StoreBoundAuthorizationHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        IStoreBoundEntity resource)
    {
        // Bỏ qua check chi nhánh nếu là Admin tổng
        if (_currentUser.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // LUẬT CỐT LÕI (DATA SCOPE / ABAC):
        // Nếu Store đang làm việc (ActiveStoreId) TRÙNG khớp với StoreId của dữ liệu -> DUYỆT!
        if (_currentUser.ActiveStoreId == resource.StoreId)
        {
            context.Succeed(requirement);
        }
        else
        {
            // Có thể log lại Cảnh báo Bảo mật (Ai đó đang cố hack sửa đơn hàng của chi nhánh khác)
        }

        return Task.CompletedTask;
    }
}