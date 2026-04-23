using Identity.Application.Features.Users.Commands;
using Identity.Application.Features.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;

namespace Identity.Presentation.Controllers;

[Route("api/identity/admin/users")]
[Authorize(Roles = "Admin,Manager")]
public class AdminUsersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? searchTerm,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPaginatedUsersQuery(pageIndex, pageSize, searchTerm, role, status);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpPatch("{publicId:guid}/lock")]
    public async Task<IActionResult> LockUser(Guid publicId, [FromBody] LockUserCommand command)
    {
        if (publicId != command.TargetUserPublicId)
            return BadRequest(new ErrorResponse("INVALID_ID", "ID trên URL và Body không khớp.")); 

        var result = await Mediator.Send(command);
        return HandleResult(result, "Đã khóa tài khoản người dùng.");
    }

    [HttpPatch("{publicId:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid publicId)
    {
        var result = await Mediator.Send(new UnlockUserCommand(publicId));
        return HandleResult(result, "Đã mở khóa tài khoản.");
    }

    [HttpPatch("{publicId:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(Guid publicId, [FromBody] ChangeUserRoleCommand command)
    {
        if (publicId != command.TargetUserPublicId)
            return BadRequest(new ErrorResponse("INVALID_ID", "ID trên URL và Body không khớp."));

        var result = await Mediator.Send(command);
        return HandleResult(result, "Cập nhật quyền thành công.");
    }
}