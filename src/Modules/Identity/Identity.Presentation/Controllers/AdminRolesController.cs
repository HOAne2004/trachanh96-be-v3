using Identity.Application.Features.Roles.Commands;
using Identity.Application.Features.Roles.Queries;
using Identity.Domain.Constants; // Nơi chứa class IdentityPermissions
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Authorization; // Nơi chứa HasPermissionAttribute
using Shared.Presentation.Controllers;

namespace Identity.Presentation.Controllers;

[Route("api/identity/admin/roles")]
public class AdminRolesController : BaseApiController
{
    // ----------------------------------------------------
    // QUẢN LÝ VAI TRÒ (ROLES)
    // ----------------------------------------------------

    [HttpGet]
    [HasPermission(IdentityPermissions.Roles.View)]
    public async Task<IActionResult> GetRoles([FromQuery] GetPaginatedRolesQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(IdentityPermissions.Roles.View)]
    public async Task<IActionResult> GetRoleById(Guid id)
    {
        var result = await Mediator.Send(new GetRoleByIdQuery(id));
        return HandleResult(result);
    }

    [HttpPost]
    [HasPermission(IdentityPermissions.Roles.Create)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Tạo Vai trò mới thành công.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(IdentityPermissions.Roles.Update)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleCommand command)
    {
        // Gắn ID từ URL vào Command để bảo đảm tính nhất quán
        var secureCommand = command with { RoleId = id };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(IdentityPermissions.Roles.Delete)]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var result = await Mediator.Send(new DeleteRoleCommand(id));
        return HandleResult(result);
    }

    // ----------------------------------------------------
    // QUẢN LÝ MA TRẬN PHÂN QUYỀN (PERMISSIONS)
    // ----------------------------------------------------

    [HttpGet("permissions/matrix")]
    [HasPermission(IdentityPermissions.Roles.View)] // Chỉ cần có quyền xem Role là được xem ma trận
    public async Task<IActionResult> GetPermissionMatrix()
    {
        var result = await Mediator.Send(new GetAllPermissionsQuery());
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(IdentityPermissions.Roles.AssignPermissions)] // Quyền gán quyền
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] List<string> permissionCodes)
    {
        var command = new UpdateRolePermissionsCommand(id, permissionCodes);
        var result = await Mediator.Send(command);
        return HandleResult(result, "Cập nhật ma trận phân quyền thành công.");
    }
}