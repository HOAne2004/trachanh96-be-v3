using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Features.Commands;
using Orders.Application.Features.Queries;
using Orders.Domain.Enums;
using System.Security.Claims;

namespace Orders.Presentation.Controllers;

[ApiController]
[Route("api/orders")] 
[Authorize] 
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    private Guid? GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDraftOrder([FromBody] CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        var currentUserId = GetUserIdFromToken();
        var finalCommand = currentUserId.HasValue ? command with { CustomerId = currentUserId } : command;

        var result = await _sender.Send(finalCommand, cancellationToken);
        return result.IsSuccess ? Ok(new { OrderId = result.Value }) : BadRequest(new { Message = result.Error });
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddOrderItems(Guid id, [FromBody] List<OrderItemRequest> items, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AddOrderItemsCommand(id, items), cancellationToken);
        return result.IsSuccess ? Ok(new { Message = "Cập nhật giỏ hàng thành công!" }) : BadRequest(new { Message = result.Error });
    }

    [HttpPut("{id:guid}/delivery-address")]
    public async Task<IActionResult> SetDeliveryAddress(Guid id, [FromBody] int addressId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetDeliveryAddressCommand(id, addressId), cancellationToken);
        return result.IsSuccess ? Ok(new { Message = "Cập nhật địa chỉ thành công!" }) : BadRequest(new { Message = result.Error });
    }

    [HttpPut("{id:guid}/dine-in-table")]
    public async Task<IActionResult> SetDineInTable(Guid id, [FromBody] Guid tableId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetDineInTableCommand(id, tableId), cancellationToken);
        return result.IsSuccess ? Ok(new { Message = "Cập nhật bàn thành công!" }) : BadRequest(new { Message = result.Error });
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid id, [FromBody] Guid paymentMethodId, [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey, CancellationToken cancellationToken)
    {
        if (idempotencyKey == Guid.Empty) return BadRequest(new { Message = "Thiếu Idempotency-Key trên Header." });

        var result = await _sender.Send(new CheckoutOrderCommand(id, paymentMethodId, idempotencyKey), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Message = result.Error });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] string reason, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelOrderCommand(id, reason, GetUserIdFromToken(), IsStaffOverride: false), cancellationToken);
        return result.IsSuccess ? Ok(new { Message = "Hủy đơn thành công!" }) : BadRequest(new { Message = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { Message = result.Error });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetCustomerOrderHistory([FromQuery] OrderStatusEnum? status, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var customerId = GetUserIdFromToken();
        if (customerId == null) return Unauthorized();

        var result = await _sender.Send(new GetCustomerOrderHistoryQuery(customerId.Value, status, pageIndex, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Message = result.Error });
    }
}