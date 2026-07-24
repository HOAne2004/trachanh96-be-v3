namespace AI.Application.DTOs
{
    public record ChatSessionDetailDto(
        Guid Id,
        Guid? UserId,
        Guid? CurrentOrderId,
        Guid? CurrentReservationId,
        bool IsLocked,
        int TotalTokensUsed,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<ChatMessageDto> Messages // Toàn bộ lịch sử tin nhắn
    );
}