using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Catalog.Application.Features.Categories.Commands;

namespace Catalog.Presentation.Controllers
{
    [ApiController]
    [Route("api/admin/catalog/categories")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : ControllerBase
    {
        private readonly ISender _sender;

        public CategoriesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { Message = result.Error });
            }
            return Ok(new
            {
                Message = "Tao danh mục thành công!",
                CategoryId = result.Value
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id)
            {
                return BadRequest(new { Message = "ID trong URL không khớp với ID trong body." });
            }
            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { Message = result.Error });
            }
            return Ok(new
            {
                Message = "Cập nhật danh mục thành công!",
                CategoryId = result.Value
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
        {
            var command = new DeleteCategoryCommand(id);
            var result = await _sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { Message = result.Error });
            }
            return Ok(new
            {
                Message = "Xóa danh mục thành công!",
                CategoryId = result.Value
            });
        }
    }
}

