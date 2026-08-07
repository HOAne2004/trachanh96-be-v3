using Identity.Application.Features.Users.Commands;
using Identity.Application.Features.Users.Queries;
using Identity.Domain.Constants; // Chứa IdentityPermissions
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Authorization; // Nếu chứa HasPermissionAttribute
using Shared.Presentation.Controllers;

namespace Identity.Presentation.Controllers;

[Route("api/identity/admin/users")]
// Dùng [HasPermission] thay thế hoàn toàn [Authorize(Roles = "...")] truyền thống
public class AdminUsersController : BaseApiController
{
    [HttpGet]
    [HasPermission(IdentityPermissions.Users.View)]
    public async Task<IActionResult> GetUsers([FromQuery] GetPaginatedUsersQuery query)
    {
        // Tự động map Query parameters từ URL vào Object Query
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(IdentityPermissions.Users.View)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id));
        return HandleResult(result);
    }

    [HttpPost]
    [HasPermission(IdentityPermissions.Users.Create)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserByAdminCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Đã tạo tài khoản nhân viên thành công.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(IdentityPermissions.Users.Update)] 
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserByAdminCommand command)
    {
        // Đảm bảo ID từ URL khớp với ID cần thao tác để tránh lỗi
        var secureCommand = command with { TargetUserId = id };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result);
    }
    [HttpPut("{id:guid}/roles")]
    [HasPermission(IdentityPermissions.Users.AssignRole)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] List<Guid> roleIds)
    {
        var result = await Mediator.Send(new AssignRolesToUserCommand(id, roleIds));
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/lock")]
    [HasPermission(IdentityPermissions.Users.Lock)]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] LockUserCommand command)
    {
        // Gắn ID từ URL vào Command để bảo vệ tính nhất quán
        var secureCommand = command with { TargetUserId = id };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/unlock")]
    [HasPermission(IdentityPermissions.Users.Lock)]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var result = await Mediator.Send(new UnlockUserCommand(id));
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(IdentityPermissions.Users.Delete)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var result = await Mediator.Send(new DeleteUserCommand(id));
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/restore")]
    [HasPermission(IdentityPermissions.Users.Delete)] // Tuỳ bạn thiết lập quyền Restore riêng biệt
    public async Task<IActionResult> RestoreUser(Guid id)
    {
        var result = await Mediator.Send(new RestoreUserCommand(id));
        return HandleResult(result);
    }
}