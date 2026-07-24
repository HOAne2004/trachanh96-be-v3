namespace AI.Application.DTOs
{
    public record ChatSessionDto(
        Guid Id,
        Guid? UserId,
        Guid? CurrentOrderId,
        Guid? CurrentReservationId,
        bool IsLocked,
        int MessageCount,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}