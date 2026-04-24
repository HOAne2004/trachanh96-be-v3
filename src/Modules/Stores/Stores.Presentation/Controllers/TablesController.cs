using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;
using Stores.Application.Features.Tables.Commands;

namespace Stores.Presentation.Controllers;

[ApiController]
[Route("api/admin/stores/{publicId:guid}/tables")]
[Authorize(Roles = "Admin")]
public class TablesController : BaseApiController
{
    [HttpPut("{tableId:int}")]
    public async Task<IActionResult> UpdateTable(Guid publicId, int tableId, [FromBody] UpdateTableCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId || tableId != command.TableId)
            return HandleResult(Result.Failure("ID không khớp."));

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Cập nhật bàn thành công!");
    }

    [HttpDelete("{tableId:int}")]
    public async Task<IActionResult> DeleteTable(Guid publicId, int tableId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteTableCommand(publicId, tableId), cancellationToken);
        return HandleResult(result, "Xóa bàn thành công!");
    }

    [HttpPatch("{tableId:int}/regenerate-qr")]
    public async Task<IActionResult> RegenerateQr(Guid publicId, int tableId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new RegenerateQrCommand(publicId, tableId), cancellationToken);
        return HandleResult(result, "Tạo mới mã QR thành công! Vui lòng in decal mới.");
    }
}