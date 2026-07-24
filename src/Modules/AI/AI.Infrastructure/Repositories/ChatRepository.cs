using AI.Application.Interfaces;
using AI.Domain.Entities;
using AI.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace AI.Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AIDbContext _dbContext;

        public ChatRepository(AIDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            return await _dbContext.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        }

        public async Task<ChatSession?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        {
            return await _dbContext.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.CurrentOrderId == orderId, cancellationToken);
        }

        public async Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken)
        {
            var exists = await _dbContext.ChatSessions.AnyAsync(s => s.Id == session.Id, cancellationToken);

            if (!exists)
            {
                await _dbContext.ChatSessions.AddAsync(session, cancellationToken);
            }
            else
            {
                _dbContext.ChatSessions.Update(session);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Triển khai hàm truy vấn danh sách (sắp xếp mới nhất lên đầu)
        public async Task<List<ChatSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.ChatSessions
                // Có thể không cần .Include(s => s.Messages) ở đây để tối ưu hiệu suất,
                // vì danh sách ở sidebar chỉ cần đếm số tin nhắn (MessageCount)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .ToListAsync(cancellationToken);
        }

        // Triển khai hàm lưu Outbox
        public async Task AddOutboxMessageAsync(string eventType, string eventContent, CancellationToken cancellationToken)
        {
            var outboxMessage = new Shared.Infrastructure.Outbox.OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = eventType,
                Content = eventContent,
                OccurredOnUtc = DateTime.UtcNow,
                ProcessedOnUtc = null // Chưa xử lý
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        }
    }
}