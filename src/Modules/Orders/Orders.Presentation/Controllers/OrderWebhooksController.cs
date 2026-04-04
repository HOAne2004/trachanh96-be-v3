using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Features.Commands;

namespace Orders.Presentation.Controllers;

[ApiController]
[Route("api/webhooks/orders")] 
[AllowAnonymous] 
public class OrderWebhooksController : ControllerBase
{
    private readonly ISender _sender;
    public OrderWebhooksController(ISender sender) => _sender = sender;

    [HttpPost("payment")]
    public async Task<IActionResult> ProcessPaymentWebhook([FromBody] ProcessPaymentWebhookCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        // Trả về 200 OK ngay cả khi lỗi nghiệp vụ (như hóa đơn đã thanh toán rồi) 
        // để đối tác không cố gắng gọi lại (retry) gây kẹt mạng.
        return result.IsSuccess ? Ok() : Ok(new { Warning = result.Error });
    }
}