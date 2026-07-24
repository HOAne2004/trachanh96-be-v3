using AI.Application.Features.Commands;
using AI.Application.Features.Queries;
using AI.Presentation.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AI.Presentation.Controllers
{
    [ApiController]
    [Route("api/ai/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ISender _mediator;

        public ChatController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một phiên đàm thoại mới (Hỗ trợ cả khách ẩn danh hoặc User đã login)
        /// </summary>
        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateChatSessionCommand(request.UserId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        /// <summary>
        /// Gửi tin nhắn và nhận phản hồi từ AI
        /// </summary>
        [HttpPost("sessions/{sessionId}/messages")]
        public async Task<IActionResult> SendMessage(Guid sessionId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
        {
            var command = new SendMessageCommand(sessionId, request.Message);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử tin nhắn của một phiên cụ thể
        /// </summary>
        [HttpGet("sessions/{sessionId}/messages")]
        public async Task<IActionResult> GetChatHistory(Guid sessionId, CancellationToken cancellationToken)
        {
            var query = new GetChatHistoryQuery(sessionId);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return NotFound(result.Error);

            return Ok(result.Value);
        }

        /// <summary>
        /// Lấy danh sách các phiên đàm thoại của một người dùng
        /// </summary>
        [HttpGet("users/{userId}/sessions")]
        public async Task<IActionResult> GetUserSessions(Guid userId, CancellationToken cancellationToken)
        {
            var query = new GetUserSessionsQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }
    }

}