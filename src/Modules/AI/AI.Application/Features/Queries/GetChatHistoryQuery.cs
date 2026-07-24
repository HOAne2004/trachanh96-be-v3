using AI.Application.DTOs;
using AI.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace AI.Application.Features.Queries
{
    // QUERY
    // Yêu cầu truyền vào ID của phiên đàm thoại, trả về danh sách các tin nhắn đã được map sang DTO
    public record GetChatHistoryQuery(Guid SessionId) : IRequest<Result<List<ChatMessageDto>>>;

    // HANDLER
    public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, Result<List<ChatMessageDto>>>
    {
        private readonly IChatRepository _chatRepository;

        public GetChatHistoryQueryHandler(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<Result<List<ChatMessageDto>>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
        {
            // 1. Lấy phiên đàm thoại kèm toàn bộ tin nhắn từ Database
            // Lưu ý: Hàm GetByIdAsync trong ChatRepository đã có sẵn .Include(s => s.Messages)
            var session = await _chatRepository.GetByIdAsync(request.SessionId, cancellationToken);

            if (session == null)
            {
                return Result<List<ChatMessageDto>>.Failure("Không tìm thấy phiên đàm thoại.");
            }

            // 2. Chuyển đổi (Mapping) từ Entity sang DTO để trả về cho Frontend
            // Ánh xạ các Enum thành chuỗi (ToString) để VueJS dễ dàng xử lý điều kiện hiển thị
            var messageDtos = session.Messages
                .OrderBy(m => m.Timestamp) // Đảm bảo tin nhắn được sắp xếp đúng thứ tự thời gian
                .Select(m => new ChatMessageDto(
                    Id: m.Id,
                    Role: m.Role.ToString(),
                    Content: m.Content,
                    Payload: m.Payload,
                    Status: m.Status.ToString(),
                    Timestamp: m.Timestamp
                ))
                .ToList();

            // 3. Trả về kết quả thành công
            return Result<List<ChatMessageDto>>.Success(messageDtos);
        }
    }
}