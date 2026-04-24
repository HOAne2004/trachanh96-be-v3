namespace Stores.Application.DTOs.Responses;

// [ADMIN] Dùng cho API: GET /api/admin/stores (Danh sách cửa hàng dạng bảng)
public record StoreAdminListDto(
    Guid PublicId,
    string StoreCode,
    string Name,
    string Status,
    string FullAddress
);

// [ADMIN] Dùng cho API: GET /api/admin/stores/{id} (Chi tiết 1 cửa hàng trong CMS)
// Lưu ý: Đây chính là file StoreDetailDto cũ được khôi phục và đổi tên cho rõ nghĩa
public record StoreAdminDetailDto(
    Guid PublicId,
    string StoreCode,
    string Name,
    string FullAddress,
    string Status,
    List<OperatingHourDto> OperatingHours, // Lịch đã cấu hình
    List<AreaDto> Areas                    // Khu vực và bàn đang có
);

// [ADMIN] Dùng cho API: GET /api/admin/stores/{id}/areas (Chỉ lấy danh sách khu vực để đếm số bàn)
// Đổi tên từ AreaResponseDto cũ để tránh nhầm với AreaDto
public record AreaAdminListDto(
    int AreaId,
    string Name,
    bool IsActive,
    int TableCount
);