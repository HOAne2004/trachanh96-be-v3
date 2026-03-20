using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stores.Application.Features.Tables.Queries;
// Thêm using cho GetCatalogStoresQuery và GetStoreBySlugQuery khi bạn tạo chúng

namespace Stores.Presentation.Controllers;

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicStoresController : ControllerBase
{
    private readonly ISender _sender;
    public PublicStoresController(ISender sender) => _sender = sender;

    [HttpGet("tables/qr/{token}")]
    public async Task<IActionResult> GetTableByQr(string token, CancellationToken cancellationToken)
    {
        var query = new GetTableByQrTokenQuery(token);
        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure) return BadRequest(new { Message = result.Error });
        return Ok(result.Value);
    }

    // Tương lai bạn sẽ thêm 2 API này vào đây:
    // [HttpGet("stores")] -> GetCatalogStores (Chỉ trả về quán Active/ComingSoon)
    // [HttpGet("stores/{slug}")] -> GetStoreBySlug (Trả về chi tiết kèm IsOpenNow)
}