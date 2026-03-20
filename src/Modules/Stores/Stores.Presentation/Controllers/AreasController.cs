using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stores.Application.Features.Areas.Commands;
using Stores.Application.Features.Areas.Queries;
using Stores.Application.Features.Tables.Commands; // Thêm dòng này

namespace Stores.Presentation.Controllers
{
    [ApiController]
    // SỬA ROUTE BASE: Buộc mọi tài nguyên Area phải đi qua Store
    [Route("api/admin/stores/{publicId:guid}/areas")]
    [Authorize(Roles = "Admin")]
    public class AreasController : ControllerBase
    {
        private readonly ISender _sender;
        public AreasController(ISender sender) => _sender = sender;

        // Route thực tế: GET api/admin/stores/{publicId}/areas
        [HttpGet]
        public async Task<IActionResult> GetAreas(Guid publicId, CancellationToken cancellationToken)
        {
            var query = new GetStoreAreasQuery(publicId);
            var result = await _sender.Send(query, cancellationToken);
            if (result.IsFailure) return NotFound(new { Message = result.Error });
            return Ok(result.Value);
        }

        // Route thực tế: POST api/admin/stores/{publicId}/areas
        [HttpPost]
        public async Task<IActionResult> AddArea(Guid publicId, [FromBody] AddAreaCommand command, CancellationToken cancellationToken)
        {
            if (publicId != command.PublicId) return BadRequest(new { Message = "ID không khớp." });
            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });
            return Ok(new { Message = "Thêm khu vực thành công!" });
        }

        // Route thực tế: PUT api/admin/stores/{publicId}/areas/{areaId}
        [HttpPut("{areaId:int}")]
        public async Task<IActionResult> UpdateArea(Guid publicId, int areaId, [FromBody] UpdateAreaCommand command, CancellationToken cancellationToken)
        {
            if (publicId != command.PublicId || areaId != command.AreaId)
                return BadRequest(new { Message = "ID trên URL và dữ liệu không khớp." });

            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });
            return Ok(new { Message = "Cập nhật khu vực thành công!" });
        }

        // Route thực tế: DELETE api/admin/stores/{publicId}/areas/{areaId}
        [HttpDelete("{areaId:int}")]
        public async Task<IActionResult> DeleteArea(Guid publicId, int areaId, CancellationToken cancellationToken)
        {
            var command = new DeleteAreaCommand(publicId, areaId);
            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });
            return Ok(new { Message = "Xóa khu vực thành công!" });
        }

        /// <summary>
        /// [ADMIN] Thêm bàn vào khu vực (CHUYỂN TỪ TablesController SANG ĐÂY)
        /// Route thực tế: POST api/admin/stores/{publicId}/areas/{areaId}/tables
        /// </summary>
        [HttpPost("{areaId:int}/tables")]
        public async Task<IActionResult> AddTable(Guid publicId, int areaId, [FromBody] AddTableCommand command, CancellationToken cancellationToken)
        {
            if (publicId != command.PublicId || areaId != command.AreaId)
                return BadRequest(new { Message = "ID trên URL và Body không khớp." });

            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });

            return Ok(new { Message = "Thêm bàn thành công!" });
        }
    }
}