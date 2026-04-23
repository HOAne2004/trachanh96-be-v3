using Catalog.Application.Features.Categories.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;

namespace Catalog.Presentation.Controllers
{
    [Route("api/admin/catalog/categories")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : BaseApiController
    {
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result, "Tạo danh mục thành công!");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id)
            {
                return BadRequest(new ErrorResponse("INVALID_ID", "ID trong URL không khớp với ID trong body."));
            }
            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result, "Cập nhật danh mục thành công!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
            return HandleResult(result, "Xóa danh mục thành công!");
        }
    }
}