namespace Stores.Application.DTOs.Responses;

// [SHARED] Dùng để hiển thị cấu hình 1 ngày hoạt động
public record OperatingHourDto(
    DayOfWeek DayOfWeek,
    string OpenTime,
    string CloseTime,
    bool IsClosed
);

// [SHARED] Dùng để hiển thị thông tin 1 Bàn (nằm trong Khu vực)
public record TableDto(
    int TableId,
    string Name,
    int SeatCapacity,
    bool IsActive
);

// [SHARED] Dùng để hiển thị Khu vực (bao gồm danh sách Bàn)
public record AreaDto
{
    public int AreaId { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<TableDto> Tables { get; init; } = new();
}