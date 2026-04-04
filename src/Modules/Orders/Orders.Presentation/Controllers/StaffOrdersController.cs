using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Features.Commands;
using Orders.Application.Features.Queries;
using Orders.Domain.Enums;
using System.Security.Claims;

namespace Orders.Presentation.Controllers
{
    [ApiController]
    [Route("api/staff/orders")]
    [Authorize(Roles = "Staff, Admin")]
    public class StaffOrdersController : ControllerBase
    {
        private readonly ISender _sender;
        public StaffOrdersController(ISender sender) => _sender = sender;

        private Guid? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        // Ghi đè Route để chuẩn URL lồng nhau: /api/staff/stores/{storeId}/orders
        [HttpGet("~/api/staff/stores/{storeId:guid}/orders")]
        public async Task<IActionResult> GetStoreOrders(Guid storeId, [FromQuery] OrderStatusEnum? status, [FromQuery] string? search, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetStoreOrdersQuery(storeId, status, search, pageIndex, pageSize), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Message = result.Error });
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken cancellationToken)
        {
            var staffId = GetUserIdFromToken();
            if (staffId == null) return Unauthorized("Không xác định được danh tính nhân viên.");

            var command = new ConfirmOrderCommand(id, staffId.Value);
            var result = await _sender.Send(command, cancellationToken);

            return result.IsSuccess ? Ok(new { Message = "Đã nhận đơn và bắt đầu pha chế!" }) : BadRequest(new { Message = result.Error });
        }

        [HttpPost("{id:guid}/ready")]
        public async Task<IActionResult> MarkOrderReady(Guid id, CancellationToken cancellationToken)
        {
            var staffId = GetUserIdFromToken();
            if (staffId == null) return Unauthorized(new { Message = "Không xác định được nhân viên." });

            var result = await _sender.Send(new MarkOrderReadyCommand(id, staffId.Value), cancellationToken);
            return result.IsSuccess ? Ok(new { Message = "Đơn hàng đã sẵn sàng giao!" }) : BadRequest(new { Message = result.Error });
        }

        [HttpPost("{id:guid}/complete")]
        public async Task<IActionResult> CompleteOrder(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new CompleteOrderCommand(id), cancellationToken);
            return result.IsSuccess ? Ok(new { Message = "Đơn hàng đã hoàn tất!" }) : BadRequest(new { Message = result.Error });
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelOrderAsStaff(Guid id, [FromBody] string reason, CancellationToken cancellationToken)
        {
            // IsStaffOverride = true để bỏ qua các quy tắc cấm hủy đơn khi đang pha chế của khách
            var result = await _sender.Send(new CancelOrderCommand(id, reason, GetUserIdFromToken(), IsStaffOverride: true), cancellationToken);
            return result.IsSuccess ? Ok(new { Message = "Hủy đơn thành công!" }) : BadRequest(new { Message = result.Error });
        }
    }
}
