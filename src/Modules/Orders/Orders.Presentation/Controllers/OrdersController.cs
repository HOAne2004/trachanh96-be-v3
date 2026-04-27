using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Features.Commands;
using Orders.Application.Features.Queries;
using Orders.Domain.Enums;
using Shared.Presentation.Controllers; // Nhớ using namespace chứa BaseApiController
using System.Security.Claims;

namespace Orders.Presentation.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : BaseApiController
{
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

        var result = await Mediator.Send(finalCommand, cancellationToken);
        return HandleResult(result, "Tạo đơn nháp thành công!");
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddOrderItems(Guid id, [FromBody] List<OrderItemRequest> items, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AddOrderItemsCommand(id, items), cancellationToken);
        return HandleResult(result, "Cập nhật giỏ hàng thành công!");
    }

    [HttpPut("{id:guid}/delivery-address")]
    public async Task<IActionResult> SetDeliveryAddress(Guid id, [FromBody] int addressId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetDeliveryAddressCommand(id, addressId), cancellationToken);
        return HandleResult(result, "Cập nhật địa chỉ thành công!");
    }

    [HttpPut("{id:guid}/dine-in-table")]
    public async Task<IActionResult> SetDineInTable(Guid id, [FromBody] Guid tableId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetDineInTableCommand(id, tableId), cancellationToken);
        return HandleResult(result, "Cập nhật bàn thành công!");
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid id, [FromBody] Guid paymentMethodId, [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey, CancellationToken cancellationToken)
    {
        if (idempotencyKey == Guid.Empty) return BadRequest(new { Message = "Thiếu Idempotency-Key trên Header." });

        var result = await Mediator.Send(new CheckoutOrderCommand(id, paymentMethodId, idempotencyKey), cancellationToken);
        return HandleResult(result, "Chốt đơn thành công!");
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] string reason, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CancelOrderCommand(id, reason, GetUserIdFromToken(), IsStaffOverride: false), cancellationToken);
        return HandleResult(result, "Hủy đơn thành công!");
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return HandleResult(result, "Lấy thông tin đơn hàng thành công!");
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetCustomerOrderHistory([FromQuery] List<OrderStatusEnum>? status, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var customerId = GetUserIdFromToken();
        if (customerId == null) return Unauthorized();

        var result = await Mediator.Send(new GetCustomerOrderHistoryQuery(customerId.Value, status, pageIndex, pageSize), cancellationToken);
        return HandleResult(result, "Lấy lịch sử đơn hàng thành công!");
    }

    [HttpGet("active-cart")]
    public async Task<IActionResult> GetActiveCart([FromQuery] Guid storeId, CancellationToken cancellationToken)
    {
        var customerId = GetUserIdFromToken();
        if (customerId == null) return Unauthorized();

        var result = await Mediator.Send(new GetActiveCartQuery(customerId.Value, storeId), cancellationToken);
        return HandleResult(result, "Lấy giỏ hàng thành công!");
    }
}