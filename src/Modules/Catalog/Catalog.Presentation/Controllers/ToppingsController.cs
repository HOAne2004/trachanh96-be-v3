using Catalog.Application.Features.Toppings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;

namespace Catalog.Presentation.Controllers
{
    [Route("api/admin/catalog/toppings")]
    [Authorize(Roles = "Admin")]
    public class ToppingsController : BaseApiController
    {
        [HttpPost]
        public async Task<IActionResult> CreateTopping([FromBody] CreateToppingCommand command, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result, "Tạo topping thành công!");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTopping(int id, [FromBody] UpdateToppingCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id)
                return BadRequest(new ErrorResponse("INVALID_ID", "ID không khớp."));

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result, "Cập nhật topping thành công!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTopping(int id, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new DeleteToppingCommand(id), cancellationToken);
            return HandleResult(result, "Xóa topping thành công!");
        }
    }
}