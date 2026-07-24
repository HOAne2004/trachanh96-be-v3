using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Outbox;
using System.Reflection;

namespace AI.Infrastructure.Database
{
    public class AIDbContext : DbContext
    {
        public AIDbContext(DbContextOptions<AIDbContext> options) : base(options) 
        {
        }

        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Thiết lập Schema mặc định cho module AI
            modelBuilder.HasDefaultSchema("ai");

            // Tự động quét và áp dụng tất cả IEntityTypeConfiguration trong Assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }
    }
}
