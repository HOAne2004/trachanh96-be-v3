using AI.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace AI.Application.Features.Commands
{
    // COMMAND
    public record AttachReservationToSessionCommand(
        Guid SessionId,
        Guid ReservationId
    ) : IRequest<Result<bool>>;

    // HANDLER
    public class AttachReservationToSessionCommandHandler : IRequestHandler<AttachReservationToSessionCommand, Result<bool>>
    {
        private readonly IChatRepository _chatRepository;

        public AttachReservationToSessionCommandHandler(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<Result<bool>> Handle(AttachReservationToSessionCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy phiên đàm thoại hiện tại
            var session = await _chatRepository.GetByIdAsync(request.SessionId, cancellationToken);

            if (session == null)
            {
                return Result<bool>.Failure("Không tìm thấy phiên đàm thoại.");
            }

            // 2. Cập nhật ReservationId thông qua hàm của Domain Entity
            session.AttachReservation(request.ReservationId);

            // 3. Lưu thay đổi xuống Database
            await _chatRepository.SaveSessionAsync(session, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}