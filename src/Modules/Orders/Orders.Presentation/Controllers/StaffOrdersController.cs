using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Features.Commands;
using Orders.Application.Features.Queries;
using Orders.Domain.Enums;
using Shared.Presentation.Controllers;
using System.Security.Claims;

namespace Orders.Presentation.Controllers
{
    [ApiController]
    [Route("api/staff/orders")]
    [Authorize(Roles = "Staff, Admin")]
    public class StaffOrdersController : BaseApiController
    {
        private Guid? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        [HttpGet("~/api/staff/stores/{storeId:guid}/orders")]
        public async Task<IActionResult> GetStoreOrders(Guid storeId, [FromQuery] OrderStatusEnum? status, [FromQuery] string? search, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetStoreOrdersQuery(storeId, status, search, pageIndex, pageSize), cancellationToken);
            return HandleResult(result, "Lấy danh sách đơn hàng thành công!");
        }

        [HttpGet("~/api/staff/stores/{storeId:guid}/dashboard-metrics")]
        public async Task<IActionResult> GetDashboardMetrics(Guid storeId, CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetStoreDashboardMetricsQuery(storeId), cancellationToken);
            return HandleResult(result, "Lấy thống kê thành công!");
        }

        [HttpPost("{id:guid}/confirm-cash")]
        public async Task<IActionResult> ConfirmCashPayment(Guid id, CancellationToken cancellationToken)
        {
            var staffId = GetUserIdFromToken();
            if (staffId == null) return Unauthorized(new { Message = "Không xác định được danh tính nhân viên." });

            var result = await Mediator.Send(new ConfirmCashPaymentCommand(id, staffId.Value), cancellationToken);
            return HandleResult(result, "Đã xác nhận thu tiền mặt thành công!");
        }

        [HttpPost("{id:guid}/confirm")]
        public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken cancellationToken)
        {
            var staffId = GetUserIdFromToken();
            if (staffId == null) return Unauthorized("Không xác định được danh tính nhân viên.");

            var result = await Mediator.Send(new ConfirmOrderCommand(id, staffId.Value), cancellationToken);
            return HandleResult(result, "Đã tiếp nhận đơn hàng!");
        }

        [HttpPost("{id:guid}/start-preparing")]
        public async Task<IActionResult> StartPreparing(Guid id, CancellationToken cancellationToken)
        {
            var staffId = GetUserIdFromToken();
            if (staffId == null) return Unauthorized("Không xác định được danh tính nhân viên.");

            var result = await Mediator.Send(new StartPreparingCommand(id, staffId.Value), cancellationToken);
            return HandleResult(result, "Bắt đầu pha chế đơn hàng!");
        }

        [HttpPost("{id:guid}/ready")]
        public async Task<IActionResult> MarkOrderReady(Guid id, CancellationToken cancellationToken)
        {
            var staffId = GetUserIdFromToken();
            if (staffId == null) return Unauthorized(new { Message = "Không xác định được nhân viên." });

            var result = await Mediator.Send(new MarkOrderReadyCommand(id, staffId.Value), cancellationToken);
            return HandleResult(result, "Đơn hàng đã pha chế xong!");
        }

        [HttpPost("{id:guid}/ship")]
        public async Task<IActionResult> ShipOrder(Guid id, CancellationToken cancellationToken)
        {
            var staffId = GetUserIdFromToken();
            if (staffId == null) return Unauthorized(new { Message = "Không xác định được nhân viên." });

            var result = await Mediator.Send(new ShipOrderCommand(id, staffId.Value), cancellationToken);
            return HandleResult(result, "Đơn hàng đang được giao!");
        }

        [HttpPost("{id:guid}/complete")]
        public async Task<IActionResult> CompleteOrder(Guid id, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new CompleteOrderCommand(id), cancellationToken);
            return HandleResult(result, "Đơn hàng đã hoàn tất!");
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelOrderAsStaff(Guid id, [FromBody] string reason, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new CancelOrderCommand(id, reason, GetUserIdFromToken(), IsStaffOverride: true), cancellationToken);
            return HandleResult(result, "Hủy đơn thành công!");
        }
    }
}