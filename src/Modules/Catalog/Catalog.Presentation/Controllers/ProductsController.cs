using Catalog.Application.Features.Products.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;

namespace Catalog.Presentation.Controllers;

[Route("api/admin/catalog/products")]
[Authorize(Roles = "Admin")]
public class ProductsController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Tạo sản phẩm thành công!");
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid publicId, [FromBody] UpdateProductCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.Id)
        {
            return BadRequest(new ErrorResponse("INVALID_ID", "ID trong URL không khớp với ID trong body."));
        }
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result, "Cập nhật sản phẩm thành công!");
    }

    [HttpDelete("{publicId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid publicId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteProductCommand(publicId), cancellationToken);
        return HandleResult(result, "Xóa sản phẩm thành công. Dữ liệu sẽ được lưu trữ tạm thời trong 30 ngày.");
    }
}