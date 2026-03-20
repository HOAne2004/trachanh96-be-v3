using Microsoft.AspNetCore.Mvc;
using MediatR;
using Catalog.Application.Features.Toppings;
using Microsoft.AspNetCore.Authorization;

namespace Catalog.Presentation.Controllers
{
    [ApiController]
    [Route("api/catalog/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ToppingsController : ControllerBase
    {
        private readonly ISender _sender;

        public ToppingsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTopping([FromBody] CreateToppingCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });

            return Ok(new { Message = "Tạo topping thành công!", ToppingId = result.Value });
        }

        [HttpPut("{id}")] // Sửa POST thành PUT cho chuẩn REST
        public async Task<IActionResult> UpdateTopping(int id, [FromBody] UpdateToppingCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id) return BadRequest(new { Message = "ID không khớp." });

            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });

            return Ok(new { Message = "Cập nhật topping thành công!" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTopping(int id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DeleteToppingCommand(id), cancellationToken);
            if (result.IsFailure) return BadRequest(new { Message = result.Error });

            return Ok(new { Message = "Xóa topping thành công!" });
        }
    }
}