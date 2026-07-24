
namespace AI.Application.DTOs
{
    public record ChatMessageDto(
        Guid Id,
        string Role,
        string Content,
        string? Payload,
        string Status,
        DateTime Timestamp);
}
