using Catalog.Application.Features.Products.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Presentation.Controllers;

namespace Catalog.Presentation.Controllers
{
    [Route("api/catalog/products")]
    [AllowAnonymous]
    public class PublicProductsController : BaseApiController
    {
        [HttpGet] // KHÔNG CÓ THAM SỐ TRÊN URL
        public async Task<IActionResult> GetCatalogProducts([FromQuery] GetCatalogProductsQuery query, CancellationToken cancellationToken)
        {
            // Gọi Query Handler mà ta đã viết
            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProductByPublicId(Guid id, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new GetProductByIdQuery(id, storeId), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetProductBySlug(string slug, [FromQuery] Guid? storeId, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new GetProductBySlugQuery(slug, storeId), cancellationToken);
            return HandleResult(result);
        }
    }
}