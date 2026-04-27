using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Features.Commands;
using Shared.Presentation.Controllers;

namespace Orders.Presentation.Controllers;

[ApiController]
[Route("api/webhooks/orders")]
[AllowAnonymous]
public class OrderWebhooksController : BaseApiController
{
    [HttpPost("payment")]
    public async Task<IActionResult> ProcessPaymentWebhook([FromBody] ProcessPaymentWebhookCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);

        // Không dùng HandleResult ở đây để đối tác không cố gắng gọi lại (retry) khi gặp mã lỗi 400.
        return result.IsSuccess ? Ok() : Ok(new { Warning = result.Error });
    }
}