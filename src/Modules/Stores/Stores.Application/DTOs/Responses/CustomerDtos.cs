namespace Stores.Application.DTOs.Responses;

// [CUSTOMER] Dùng cho API: GET /api/stores (Trang Chọn quán / Hệ thống cửa hàng)
public record StoreCustomerListDto(
    Guid PublicId,
    string Slug,
    string Name,
    string FullAddress,
    string? ImageUrl,
    double? DistanceKm,          // Tính bằng Haversine
    bool IsOpenNow,              // Trạng thái mở/đóng hiện tại
    string? ClosingTimeToday,    // Giờ đóng cửa hôm nay (VD: 22:30)
    DateTime? OpenDate           // Phục vụ làm Timeline Lịch sử hình thành
);

// [CUSTOMER] Dùng cho API: GET /api/stores/{slug} (Trang Chi tiết quán / Menu / Đặt bàn)
public record StoreCustomerDetailDto(
    Guid PublicId,
    string Name,
    string Slug,
    string FullAddress,
    string? PhoneNumber,
    string? WifiPassword,
    string? Description,
    string? ImageUrl,

    // Hiển thị nhãn trạng thái trực tiếp
    bool IsOpenNow,
    string? OpenTimeToday,
    string? ClosingTimeToday,
    DateTime? OpenDate,

    List<OperatingHourDto> WeeklySchedule, // Hiển thị bảng giờ hoạt động trong tuần
    List<AreaDto> Areas                    // Sơ đồ bàn để khách hàng đặt trước (Booking)
);

// [CUSTOMER] Dùng cho API: GET /api/stores/table-qr/{token} (Khách hàng quét mã QR tại bàn)
public record TableQrScanResponseDto(
    Guid StorePublicId,
    string StoreName,
    int AreaId,
    string AreaName,
    int TableId,
    string TableName,
    int SeatCapacity
);