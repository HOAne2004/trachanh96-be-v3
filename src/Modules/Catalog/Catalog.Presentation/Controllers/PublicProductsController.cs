
using Catalog.Application.Features.Products.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Catalog.Presentation.Controllers
{
    [ApiController]
    [Route("api/catalog/products")]
    [AllowAnonymous]
    public class PublicProductsController : ControllerBase
    {
        private ISender _sender;
        public PublicProductsController(ISender sender)
        {
            _sender = sender;
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProductByPublicId(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { Message = result.Error });
            }

            var value = result.GetType().GetProperty("Value")?.GetValue(result);
            return Ok(value ?? (object)result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetProductBySlug(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { Message = result.Error });
            }

            var value = result.GetType().GetProperty("Value")?.GetValue(result);
            return Ok(value ?? (object)result);
        }

    }
}
