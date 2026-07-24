using AI.Application.DTOs;
using AI.Application.Interfaces;
using AI.Domain.Entities;
using AI.Domain.Enums;
using Catalog.Application.Features.Products.Queries;
using FluentValidation;
using MediatR;
using Shared.Application.IntegrationEvents;
using Shared.Application.Models;
using System.Text.Json;

namespace AI.Application.Features.Commands
{
    // COMMAND
    public record SendMessageCommand(
        Guid SessionId,
        string Message
    ) : IRequest<Result<AIConversationResult>>;

    // VALIDATOR
    public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageCommandValidator()
        {
            // Kiểm tra SessionId
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("SessionId không được để trống.");

            // Kiểm tra nội dung tin nhắn (Message)
            RuleFor(x => x.Message)
                .Cascade(CascadeMode.Stop) // Dừng kiểm tra ngay nếu dính lỗi đầu tiên để tránh check độ dài chuỗi null
                .NotEmpty().WithMessage("Nội dung tin nhắn không được để trống.")
                .MaximumLength(1000).WithMessage("Nội dung tin nhắn quá dài. Vui lòng nhập dưới 1000 ký tự.");

            // Bổ sung thêm rule chặn các ký tự độc hại nếu cần (Tùy chọn)
            // .Matches(@"^[^<>]*$").WithMessage("Nội dung chứa ký tự không hợp lệ.");
        }
    }
    // HANDLER
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<AIConversationResult>>
    {
        private readonly IAIService _aiService;
        private readonly ISender _mediator;
        private readonly IChatRepository _chatRepository;

        public SendMessageCommandHandler(IAIService aiService, ISender mediator, IChatRepository chatRepository)
        {
            _aiService = aiService;
            _mediator = mediator;
            _chatRepository = chatRepository;
        }

        public async Task<Result<AIConversationResult>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            // 1. LẤY PHIÊN CHAT
            var session = await _chatRepository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                return Result<AIConversationResult>.Failure("Phiên hội thoại không tồn tại. Vui lòng tạo phiên mới.");
            }

            // 2. KIỂM TRA KHÓA CONCURRENCY (CHỐNG SPAM)
            if (session.IsLocked)
            {
                return Result<AIConversationResult>.Failure("Vui lòng chờ AI trả lời tin nhắn trước đó.");
            }

            // Khóa phiên và lưu trạng thái xuống DB ngay lập tức để chặn các request đến sau
            session.LockSession();
            await _chatRepository.SaveSessionAsync(session, cancellationToken);

            try
            {
                // 3. LƯU TIN NHẮN CỦA USER VÀO BỘ NHỚ
                session.AddMessage(MessageRoleEnum.User, request.Message);

                // 4. CHUẨN BỊ NGỮ CẢNH (RAG MENU)
                var catalogResult = await _mediator.Send(new GetCatalogProductsQuery(PageSize: 50), cancellationToken);
                string systemContext = string.Empty;

                if (catalogResult.IsSuccess && catalogResult.Value != null)
                {
                    // Lọc bớt dữ liệu thừa để tiết kiệm Token gửi lên Gemini
                    var simplifiedMenu = catalogResult.Value.Items.Select(p => new { p.Id, p.Name, p.BasePrice, p.status }).ToList();
                    systemContext = JsonSerializer.Serialize(simplifiedMenu);
                }

                // 5. CHUẨN BỊ LỊCH SỬ CHAT (Chỉ truyền Role và Content theo MessageDto)
                var historyDtos = session.Messages.Select(m => new MessageDto(
                    Role: m.Role.ToString(),
                    Content: m.Content
                )).ToList();

                // 6. GỌI GEMINI API
                var aiResult = await _aiService.SendMessageAsync(session.Id.ToString(), historyDtos, systemContext, cancellationToken);

                // 7. XỬ LÝ KẾT QUẢ 
                if (!aiResult.IsSuccess)
                {
                    // Xử lý khi bị bộ lọc an toàn chặn hoặc API báo lỗi
                    var failedMessage = session.AddMessage(
                        role: MessageRoleEnum.Model,
                        content: "Xin lỗi, tôi đang gặp sự cố kết nối hoặc câu hỏi không hợp lệ.",
                        status: string.IsNullOrEmpty(aiResult.BlockReason) ? MessageStatusEnum.Failed : MessageStatusEnum.Blocked,
                        promptTokens: aiResult.PromptTokens,
                        completionTokens: aiResult.CompletionTokens
                    );

                    failedMessage.MarkAsFailed(aiResult.ErrorMessage ?? aiResult.BlockReason ?? "Unknown Error");
                }
                else
                {
                    // AI phản hồi thành công và YÊU CẦU GỌI HÀM (Function Calling)
                    if (aiResult.RequiresAction && aiResult.ActionName == "AddToCart")
                    {
                        // 1. Khởi tạo Integration Event
                        var integrationEvent = new OrderIntentDetectedIntegrationEvent(
                            SessionId: session.Id,
                            OrderItemsJson: aiResult.ActionArguments ?? "[]",
                            OccurredOn: DateTime.UtcNow
                        );

                        // 2. Serialize event thành chuỗi JSON
                        var eventContent = JsonSerializer.Serialize(integrationEvent);
                        var eventType = integrationEvent.GetType().AssemblyQualifiedName ?? integrationEvent.GetType().Name;

                        // 3. Thêm vào Outbox (Lưu ý: Chưa SaveChanges ngay, nó sẽ được Save chung với Session ở khối finally)
                        await _chatRepository.AddOutboxMessageAsync(eventType, eventContent, cancellationToken);

                        // 4. Phản hồi cho người dùng
                        aiResult.TextResponse = "Dạ, em đã ghi nhận yêu cầu vào giỏ hàng. Anh/chị xem qua menu cần thêm gì nữa không ạ?";
                    }

                    else if (aiResult.RequiresAction && aiResult.ActionName == "ReserveTable")
                    {
                        // 1. Khởi tạo Integration Event cho Đặt bàn
                        var reservationEvent = new ReservationIntentDetectedIntegrationEvent(
                            SessionId: session.Id,
                            ReservationDetailsJson: aiResult.ActionArguments ?? "{}",
                            OccurredOn: DateTime.UtcNow
                        );

                        var eventContent = JsonSerializer.Serialize(reservationEvent);
                        var eventType = reservationEvent.GetType().AssemblyQualifiedName ?? reservationEvent.GetType().Name;

                        // 2. Lưu vào Outbox để module Stores bắt lấy và xử lý
                        await _chatRepository.AddOutboxMessageAsync(eventType, eventContent, cancellationToken);

                        // 3. Chuẩn bị Payload cho VueJS render form xác nhận giữ chỗ
                        aiResult.TextResponse = "Dạ, em đã tìm được bàn trống phù hợp. Anh/chị xác nhận lại thông tin đặt bàn giúp em nhé!";
                        // (VueJS sẽ dựa vào ActionName hoặc Payload này để hiển thị Generative UI tương ứng)
                    }

                    // Lưu tin nhắn thành công của AI (kèm theo Payload JSON nếu có)
                    if (!string.IsNullOrEmpty(aiResult.TextResponse))
                    {
                        session.AddMessage(
                            role: MessageRoleEnum.Model,
                            content: aiResult.TextResponse,
                            status: MessageStatusEnum.Success,
                            payload: aiResult.Payload ?? (aiResult.RequiresAction ? aiResult.ActionArguments : null), // Lưu lại tham số JSON AI sinh ra để dễ debug hoặc render UI
                            promptTokens: aiResult.PromptTokens,
                            completionTokens: aiResult.CompletionTokens
                        );
                    }
                }

                return Result<AIConversationResult>.Success(aiResult);
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi ngoại lệ (Timeout, đứt cáp...)
                var exceptionMsg = session.AddMessage(
                    role: MessageRoleEnum.Model,
                    content: "Hệ thống đang bận, vui lòng thử lại sau giây lát.",
                    status: MessageStatusEnum.Failed
                );
                exceptionMsg.MarkAsFailed(ex.Message);

                return Result<AIConversationResult>.Failure($"Lỗi xử lý hội thoại: {ex.Message}");
            }
            finally
            {
                // 8. BẮT BUỘC MỞ KHÓA VÀ LƯU DATABASE
                // Dù API Google gọi thành công, bị timeout, hay lỗi logic, 
                // khối finally này đảm bảo Session luôn được mở khóa để khách có thể chat tiếp.
                session.UnlockSession();
                await _chatRepository.SaveSessionAsync(session, cancellationToken);
            }
        }
    }
}