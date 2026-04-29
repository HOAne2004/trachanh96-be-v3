using AI.Application.DTOs;
using AI.Application.Features.Commands;
using AI.Presentation.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;

namespace AI.Presentation.Controllers
{
    [Route("api/ai/chat")]
    public class ChatController : BaseApiController
    {
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            // KIỂM TRA ĐẦU VÀO
            // Mẹo: Tận dụng luôn HandleResult để chuẩn hóa cả lỗi Validate đầu vào
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return HandleResult(Result<AIConversationResult>.Failure("Tin nhắn không được để trống."));
            }

            if (request.OrderId == Guid.Empty)
            {
                return HandleResult(Result<AIConversationResult>.Failure("Thiếu ID của phiên giao dịch (OrderId)."));
            }

            // GỌI MEDIATOR
            var command = new SendMessageCommand(request.OrderId, request.Message);
            // Dùng trực tiếp thuộc tính 'Mediator' từ lớp cha
            var result = await Mediator.Send(command, cancellationToken);

            // TRẢ KẾT QUẢ
            // Tự động bọc dữ liệu vào ApiResponse<T>
            return HandleResult(result, "AI đã phản hồi thành công");
        }
    }
}
