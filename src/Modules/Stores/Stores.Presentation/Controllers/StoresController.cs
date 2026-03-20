using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stores.Application.Features.Stores.Commands;
using Stores.Application.Features.Stores.Queries;
using Stores.Domain.Enums;

namespace Stores.Presentation.Controllers;

[ApiController]
[Route("api/admin/stores/[controller]")] 
[Authorize(Roles = "Admin")]
public class StoresController : ControllerBase
{
    private readonly ISender _sender;

    public StoresController(ISender sender)
    {
        _sender = sender;
    }

    // --- QUERIES (GET) ---

    /// <summary>
    /// [ADMIN] Lấy danh sách cửa hàng có phân trang
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStores(
        [FromQuery] string? searchTerm,
        [FromQuery] StoreStatusEnum? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAdminStoresQuery(searchTerm, status, pageIndex, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure) return BadRequest(new { Message = result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// [ADMIN] Lấy chi tiết toàn bộ cấu hình của một cửa hàng
    /// </summary>
    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> GetStoreDetail(Guid publicId, CancellationToken cancellationToken)
    {
        var query = new GetStoreDetailQuery(publicId);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure) return NotFound(new { Message = result.Error });
        return Ok(result.Value);
    }

    // --- COMMANDS (POST, PUT, PATCH, DELETE) ---

    [HttpPost]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { Message = result.Error });

        return CreatedAtAction(nameof(GetStoreDetail), new { publicId = result.Value }, new { Message = "Khởi tạo thành công", StoreId = result.Value });
    }

    [HttpPut("{publicId:guid}/general")]
    public async Task<IActionResult> UpdateGeneralInfo(Guid publicId, [FromBody] UpdateStoreGeneralInfoCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return BadRequest(new { Message = "ID không khớp." });
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { Message = result.Error });
        return Ok(new { Message = "Cập nhật thành công!" });
    }

    [HttpPut("{publicId:guid}/location")]
    public async Task<IActionResult> UpdateLocation(Guid publicId, [FromBody] UpdateStoreLocationCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return BadRequest(new { Message = "ID không khớp." });
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { Message = result.Error });
        return Ok(new { Message = "Cập nhật vị trí thành công!" });
    }

    [HttpPut("{publicId:guid}/delivery-policy")]
    public async Task<IActionResult> UpdateDeliveryPolicy(Guid publicId, [FromBody] UpdateStoreDeliveryPolicyCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return BadRequest(new { Message = "ID không khớp." });
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { Message = result.Error });
        return Ok(new { Message = "Cập nhật chính sách giao hàng thành công!" });
    }

    [HttpPut("{publicId:guid}/operating-hours")]
    public async Task<IActionResult> SetOperatingHours(Guid publicId, [FromBody] SetStoreOperatingHoursCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return BadRequest(new { Message = "ID không khớp." });
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { Message = result.Error });
        return Ok(new { Message = "Cập nhật giờ mở cửa thành công!" });
    }

    [HttpPatch("{publicId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid publicId, [FromBody] ChangeStoreStatusCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.PublicId) return BadRequest(new { Message = "ID không khớp." });
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { Message = result.Error });
        return Ok(new { Message = "Đổi trạng thái thành công!" });
    }

    [HttpDelete("{publicId:guid}")]
    public async Task<IActionResult> DeleteStore(Guid publicId, CancellationToken cancellationToken)
    {
        var command = new DeleteStoreCommand(publicId);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { Message = result.Error });
        return Ok(new { Message = "Xóa cửa hàng thành công!" });
    }
}