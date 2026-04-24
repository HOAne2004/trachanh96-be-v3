using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stores.Application.Features.Stores.Queries;
using Stores.Application.Features.Tables.Queries;
using Shared.Presentation.Controllers;

namespace Stores.Presentation.Controllers;

[ApiController]
[Route("api/stores")]
[AllowAnonymous]
public class PublicStoresController : BaseApiController
{
    /// <summary>
    /// API dành cho khách hàng: Lấy danh sách các quán đang hoạt động, có tính khoảng cách và trạng thái đóng/mở
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomerStores([FromQuery] GetCustomerStoresQuery query, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// API dành cho khách Dine-in: Quét mã QR tại bàn để lấy thông tin Quán, Khu vực, Bàn
    /// </summary>
    [HttpGet("table-qr/{token}")]
    public async Task<IActionResult> GetTableByQrToken(string token, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTableByQrTokenQuery(token), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// API dành cho khách hàng: Lấy chi tiết 1 quán bằng Slug
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCustomerStoreBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCustomerStoreBySlugQuery(slug), cancellationToken);
        return HandleResult(result);
    }
}