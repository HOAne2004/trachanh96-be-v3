using AI.Domain.Entities;

namespace AI.Application.Interfaces
{
    public interface IChatRepository
    {
        Task<ChatSession?> GetSessionAsync(Guid orderId, CancellationToken cancellationToken);
        Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken);
    }
}
