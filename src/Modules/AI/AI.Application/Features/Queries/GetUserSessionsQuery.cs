using AI.Application.DTOs;
using AI.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace AI.Application.Features.Queries
{
    // QUERY
    // Chỉ cần truyền vào UserId của khách hàng đang đăng nhập
    public record GetUserSessionsQuery(Guid UserId) : IRequest<Result<List<ChatSessionDto>>>;

    // HANDLER
    public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, Result<List<ChatSessionDto>>>
    {
        private readonly IChatRepository _chatRepository;

        public GetUserSessionsQueryHandler(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<Result<List<ChatSessionDto>>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
        {
            // 1. Lấy danh sách các phiên chat của User từ Database
            var sessions = await _chatRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (sessions == null || !sessions.Any())
            {
                // Nếu chưa có lịch sử, trả về list rỗng thay vì báo lỗi
                return Result<List<ChatSessionDto>>.Success(new List<ChatSessionDto>());
            }

            // 2. Chuyển đổi từ Entity sang DTO để trả về cho Frontend
            var sessionDtos = sessions.Select(s => new ChatSessionDto(
                Id: s.Id,
                UserId: s.UserId,
                CurrentOrderId: s.CurrentOrderId,
                CurrentReservationId: s.CurrentReservationId,
                IsLocked: s.IsLocked,
                MessageCount: s.MessageCount,
                CreatedAt: s.CreatedAt,
                UpdatedAt: s.UpdatedAt
            )).ToList();

            // 3. Trả về kết quả
            return Result<List<ChatSessionDto>>.Success(sessionDtos);
        }
    }
}