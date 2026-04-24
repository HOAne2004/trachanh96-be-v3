using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;
using Stores.Application.Features.Areas.Commands;
using Stores.Application.Features.Areas.Queries;
using Stores.Application.Features.Tables.Commands;

namespace Stores.Presentation.Controllers;

[ApiController]
[Route("api/admin/stores/{publicId:guid}/areas")]
[Authorize(Roles = "Admin")]
public class AreasController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAreas(Guid publicId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetStoreAreasQuery(publicId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddArea(Guid publicId, [FromBody] AddAreaCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId)
            return HandleResult(Result.Failure("ID không khớp."));

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Thêm khu vực thành công!");
    }

    [HttpPut("{areaId:int}")]
    public async Task<IActionResult> UpdateArea(Guid publicId, int areaId, [FromBody] UpdateAreaCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId || areaId != command.AreaId)
            return HandleResult(Result.Failure("ID trên URL và dữ liệu không khớp."));

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Cập nhật khu vực thành công!");
    }

    [HttpDelete("{areaId:int}")]
    public async Task<IActionResult> DeleteArea(Guid publicId, int areaId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteAreaCommand(publicId, areaId), cancellationToken);
        return HandleResult(result, "Xóa khu vực thành công!");
    }

    [HttpPost("{areaId:int}/tables")]
    public async Task<IActionResult> AddTable(Guid publicId, int areaId, [FromBody] AddTableCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId || areaId != command.AreaId)
            return HandleResult(Result.Failure("ID trên URL và Body không khớp."));

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Thêm bàn thành công!");
    }
}