namespace Stores.Application.DTOs.Responses;

public record StoreCustomerListDto(
    Guid PublicId,
    string Name,
    string FullAddress,
    string? ImageUrl,

    // Xử lý logic khoảng cách
    double? DistanceKm,

    // Xử lý logic thời gian hoạt động
    bool IsOpenNow,
    string? ClosingTimeToday // Trả về dạng string "22:30" hoặc null nếu hôm nay quán nghỉ
);