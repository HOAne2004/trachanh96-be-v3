using Catalog.Application.Features.Categories.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Presentation.Controllers;

namespace Catalog.Presentation.Controllers
{
    [Route("api/catalog/categories")]
    [AllowAnonymous]
    public class PublicCategoryController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new GetCategoriesQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
            return HandleResult(result);
        }
    }
}