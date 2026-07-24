using AI.Application.DTOs;
using AI.Application.Interfaces;
using AI.Domain.Entities;
using MediatR;
using Shared.Application.Models; // Sử dụng Result pattern chung của dự án

namespace AI.Application.Features.Commands
{
    // COMMAND
    // Tham số UserId là nullable (Guid?) để hỗ trợ cả khách chưa đăng nhập
    public record CreateChatSessionCommand(Guid? UserId = null) : IRequest<Result<ChatSessionDto>>;

    // HANDLER
    public class CreateChatSessionCommandHandler : IRequestHandler<CreateChatSessionCommand, Result<ChatSessionDto>>
    {
        private readonly IChatRepository _chatRepository;

        public CreateChatSessionCommandHandler(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<Result<ChatSessionDto>> Handle(CreateChatSessionCommand request, CancellationToken cancellationToken)
        {
            // 1. Khởi tạo Entity Session mới qua constructor của Domain
            var session = new ChatSession(request.UserId);

            // 2. Lưu vào Database
            await _chatRepository.SaveSessionAsync(session, cancellationToken);

            // 3. Ánh xạ (Map) sang DTO để trả về cho Presentation Layer
            var sessionDto = new ChatSessionDto(
                Id: session.Id,
                UserId: session.UserId,
                CurrentOrderId: session.CurrentOrderId,
                CurrentReservationId: session.CurrentReservationId,
                IsLocked: session.IsLocked,
                MessageCount: session.MessageCount,
                CreatedAt: session.CreatedAt,
                UpdatedAt: session.UpdatedAt
            );

            return Result<ChatSessionDto>.Success(sessionDto);
        }
    }
}