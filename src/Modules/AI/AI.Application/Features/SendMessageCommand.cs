using AI.Application.DTOs;
using AI.Application.Interfaces;
using AI.Domain.Entities;
using AI.Domain.Enums;
using Catalog.Application.Features.Products.Queries;
using MediatR;
using Orders.Application.Features.Commands;
using Shared.Application.Models;
using System.Text.Json;
using System.Text.Json.Nodes; // Thêm thư viện này để bóc tách JSON Node

namespace AI.Application.Features.Commands
{
    public record SendMessageCommand(
        Guid OrderId,     // ID của Draft Order (đóng vai trò là Giỏ hàng và Session ID)
        string Message    // Câu hỏi/Yêu cầu của khách hàng
    ) : IRequest<Result<AIConversationResult>>;

    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<AIConversationResult>>
    {
        private readonly IAIService _aiService;
        private readonly ISender _mediator;
        private readonly IChatRepository _chatRepository; // Mới thêm

        public SendMessageCommandHandler(IAIService aiService, ISender mediator, IChatRepository chatRepository)
        {
            _aiService = aiService;
            _mediator = mediator;
            _chatRepository = chatRepository;
        }

        public async Task<Result<AIConversationResult>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            // 1. LẤY HOẶC TẠO PHIÊN CHAT MỚI
            var session = await _chatRepository.GetSessionAsync(request.OrderId, cancellationToken)
                          ?? new ChatSession(request.OrderId);

            // Nạp tin nhắn mới nhất của User vào bộ nhớ
            session.AddMessage(MessageRoleEnum.User, request.Message);

            // 2. CHUẨN BỊ NGỮ CẢNH (RAG MENU)
            var catalogResult = await _mediator.Send(new GetCatalogProductsQuery(PageSize: 50), cancellationToken);
            if (!catalogResult.IsSuccess || catalogResult.Value == null)
                return Result<AIConversationResult>.Failure("Lỗi lấy Menu.");

            var simplifiedMenu = catalogResult.Value.Items.Select(p => new { p.Id, p.Name, p.BasePrice, p.status }).ToList();
            string systemContext = JsonSerializer.Serialize(simplifiedMenu);

            // 3. CHUẨN BỊ LỊCH SỬ CHAT ĐỂ GỬI CHO AI
            var historyDtos = session.Messages.Select(m => new MessageDto(
                Role: m.Role.ToString(),
                Content: m.Content
            )).ToList();

            // 4. GỌI AI
            var aiResult = await _aiService.SendMessageAsync(request.OrderId.ToString(), historyDtos, systemContext);

            // 5. XỬ LÝ KẾT QUẢ VÀ LƯU LẠI NHỮNG GÌ AI ĐÃ NÓI
            if (aiResult.RequiresAction && aiResult.ActionName == "AddToCart")
            {
                // ... (Đoạn mã parse JSON OrderItems và gọi lệnh AddToCartCommand như cũ)
                // Giả sử sau khi thêm giỏ hàng thành công:
                aiResult.TextResponse = "Dạ, em đã thêm thành công vào giỏ hàng cho mình rồi ạ!";
            }

            // BẮT BUỘC: Lưu câu trả lời của AI vào Session để lần sau AI còn nhớ
            if (!string.IsNullOrEmpty(aiResult.TextResponse))
            {
                session.AddMessage(MessageRoleEnum.Model, aiResult.TextResponse);
            }

            // 6. LƯU VÀO DATABASE
            await _chatRepository.SaveSessionAsync(session, cancellationToken);

            return Result<AIConversationResult>.Success(aiResult);
        }
    }
}