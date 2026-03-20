namespace Stores.Application.DTOs.Responses;

public record StoreDetailDto
{
    public Guid PublicId { get; init; }
    public string StoreCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string FullAddress { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    // Nested DTOs
    public List<OperatingHourResponseDto> OperatingHours { get; init; } = new();
    public List<AreaDto> Areas { get; init; } = new();
}

public record OperatingHourResponseDto(DayOfWeek DayOfWeek, string OpenTime, string CloseTime, bool IsClosed);

public record AreaDto
{
    public int AreaId { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<TableDto> Tables { get; init; } = new();
}

public record TableDto(int TableId, string Name, int SeatCapacity, bool IsActive);