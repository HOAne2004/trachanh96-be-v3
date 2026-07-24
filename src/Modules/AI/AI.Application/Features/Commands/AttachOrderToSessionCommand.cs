using AI.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace AI.Application.Features.Commands
{
    // COMMAND
    public record AttachOrderToSessionCommand(
        Guid SessionId,
        Guid OrderId
    ) : IRequest<Result<bool>>;

    // HANDLER
    public class AttachOrderToSessionCommandHandler : IRequestHandler<AttachOrderToSessionCommand, Result<bool>>
    {
        private readonly IChatRepository _chatRepository;

        public AttachOrderToSessionCommandHandler(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<Result<bool>> Handle(AttachOrderToSessionCommand request, CancellationToken cancellationToken)
        {
            var session = await _chatRepository.GetByIdAsync(request.SessionId, cancellationToken);

            if (session == null)
            {
                return Result<bool>.Failure("Không tìm thấy phiên đàm thoại.");
            }

            // Gọi hàm từ Domain Entity để cập nhật OrderId
            session.AttachOrder(request.OrderId);

            await _chatRepository.SaveSessionAsync(session, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}