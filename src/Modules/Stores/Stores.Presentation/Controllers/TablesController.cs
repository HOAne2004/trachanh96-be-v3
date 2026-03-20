using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stores.Application.Features.Tables.Commands;

namespace Stores.Presentation.Controllers
{
    [ApiController]
    // SỬA ROUTE BASE
    [Route("api/admin/stores/{publicId:guid}/tables")]
    [Authorize(Roles = "Admin")]
    public class TablesController : ControllerBase
    {
        private readonly ISender _sender;
        public TablesController(ISender sender) => _sender = sender;

        // Route thực tế: PUT api/admin/stores/{publicId}/tables/{tableId}
        [HttpPut("{tableId:int}")]
        public async Task<IActionResult> UpdateTable(Guid publicId, int tableId, [FromBody] UpdateTableCommand command, CancellationToken cancellationToken)
        {
            if (publicId != command.PublicId || tableId != command.TableId)
                return BadRequest(new { Message = "ID không khớp." });

            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });
            return Ok(new { Message = "Cập nhật bàn thành công!" });
        }

        // Route thực tế: DELETE api/admin/stores/{publicId}/tables/{tableId}
        [HttpDelete("{tableId:int}")]
        public async Task<IActionResult> DeleteTable(Guid publicId, int tableId, CancellationToken cancellationToken)
        {
            var command = new DeleteTableCommand(publicId, tableId);
            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });
            return Ok(new { Message = "Xóa bàn thành công!" });
        }

        // Route thực tế: PATCH api/admin/stores/{publicId}/tables/{tableId}/regenerate-qr
        [HttpPatch("{tableId:int}/regenerate-qr")]
        public async Task<IActionResult> RegenerateQr(Guid publicId, int tableId, CancellationToken cancellationToken)
        {
            var command = new RegenerateQrCommand(publicId, tableId);
            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });
            return Ok(new { Message = "Tạo mới mã QR thành công! Vui lòng in decal mới." });
        }
    }
}