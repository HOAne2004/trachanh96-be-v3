using Catalog.Application.Features.Products.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Presentation.Controllers;

[ApiController]
[Route("api/admin/catalog/products")]
[Authorize (Roles = "Admin")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { Message = result.Error });
        }

        // Try to extract a `Value` property if present (e.g., Result<T>), otherwise fall back to returning the result itself.
        var value = result.GetType().GetProperty("Value")?.GetValue(result);

        return Ok(new
        {
            Message = "Tạo sản phẩm thành công!",
            ProductId = value
        });
    }

    
    [HttpPut ("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid publicId, [FromBody] UpdateProductCommand command, CancellationToken cancellationToken)
    {
        if (publicId != command.Id)
        {
            return BadRequest(new { Message = "ID trong URL không khớp với ID trong body." });
        }
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { Message = result.Error });
        }

        var value = result.GetType().GetProperty("Value")?.GetValue(result);

        return Ok(new
        {
            Message = "Cập nhật sản phẩm thành công!",
            ProductId = value
        });
    }
     [HttpDelete("{id:guid}")]
     public async Task<IActionResult> DeleteProduct(Guid publicId, CancellationToken cancellationToken)
     {
         var command = new DeleteProductCommand(publicId);
         var result = await _sender.Send(command, cancellationToken);
         if (result.IsFailure)
         {
             return BadRequest(new { Message = result.Error });
         }
        return Ok(new { Message = "Xóa sản phẩm thành công. Dữ liệu sẽ được lưu trữ tạm thời trong 30 ngày." });
    }
}