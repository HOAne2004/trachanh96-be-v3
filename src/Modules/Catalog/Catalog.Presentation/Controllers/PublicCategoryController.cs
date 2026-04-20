using MediatR;
using Microsoft.AspNetCore.Mvc;
using Catalog.Application.Features.Categories.Queries;
using Microsoft.AspNetCore.Authorization;
namespace Catalog.Presentation.Controllers
{
    [ApiController]
    [Route("api/catalog/categories")]
    [AllowAnonymous]
    public class PublicCategoryController : ControllerBase
    {
        private readonly ISender _sender;

        public PublicCategoryController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetCategoriesQuery(), cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { Message = result.Error });
            }
            var value = result.GetType().GetProperty("Value")?.GetValue(result);
            return Ok(value ?? (object)result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(new { Message = result.Error });
            }
            var value = result.GetType().GetProperty("Value")?.GetValue(result);
            return Ok(value ?? (object)result);
        }
    }
}