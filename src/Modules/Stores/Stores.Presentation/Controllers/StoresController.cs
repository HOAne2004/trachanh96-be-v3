using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;
using Stores.Application.Features.Stores.Commands;
using Stores.Application.Features.Stores.Queries;
using Stores.Domain.Enums;

namespace Stores.Presentation.Controllers;

[ApiController]
[Route("api/admin/stores")]
[Authorize(Roles = "Admin")]
public class StoresController : BaseApiController
{
    // --- QUERIES (GET) ---

    [HttpGet]
    public async Task<IActionResult> GetStores(
        [FromQuery] string? searchTerm,
        [FromQuery] StoreStatusEnum? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetAdminStoresQuery(searchTerm, status, pageIndex, pageSize), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> GetStoreDetail(Guid publicId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetStoreDetailQuery(publicId), cancellationToken);
        return HandleResult(result);
    }

    // --- COMMANDS (POST, PUT, PATCH, DELETE) ---

    [HttpPost]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        // Trả về chung format ApiResponse, Data chứa Guid của cửa hàng mới
        return HandleResult(result, "Khởi tạo thành công");
    }

    [HttpPut("{publicId:guid}/general")]
    public async Task<IActionResult> UpdateGeneralInfo(Guid publicId, [FromBody] UpdateStoreGeneralInfoCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return HandleResult(Result.Failure("ID không khớp."));
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Cập nhật thông tin chung thành công!");
    }

    [HttpPut("{publicId:guid}/location")]
    public async Task<IActionResult> UpdateLocation(Guid publicId, [FromBody] UpdateStoreLocationCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return HandleResult(Result.Failure("ID không khớp."));
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Cập nhật vị trí thành công!");
    }

    [HttpPut("{publicId:guid}/delivery-policy")]
    public async Task<IActionResult> UpdateDeliveryPolicy(Guid publicId, [FromBody] UpdateStoreDeliveryPolicyCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return HandleResult(Result.Failure("ID không khớp."));
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Cập nhật chính sách giao hàng thành công!");
    }

    [HttpPut("{publicId:guid}/operating-hours")]
    public async Task<IActionResult> SetOperatingHours(Guid publicId, [FromBody] SetStoreOperatingHoursCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return HandleResult(Result.Failure("ID không khớp."));
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Cập nhật giờ mở cửa thành công!");
    }

    [HttpPatch("{publicId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid publicId, [FromBody] ChangeStoreStatusCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return HandleResult(Result.Failure("ID không khớp."));
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Đổi trạng thái thành công!");
    }

    [HttpDelete("{publicId:guid}")]
    public async Task<IActionResult> DeleteStore(Guid publicId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteStoreCommand(publicId), cancellationToken);
        return HandleResult(result, "Xóa cửa hàng thành công!");
    }
}