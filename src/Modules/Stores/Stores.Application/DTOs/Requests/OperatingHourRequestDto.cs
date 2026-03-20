
namespace Stores.Application.DTOs.Requests
{
    public record OperatingHourRequestDto(
        DayOfWeek DayOfWeek,
        TimeSpan? OpenTime,
        TimeSpan? CloseTime,
        bool IsClosed
    );
}
