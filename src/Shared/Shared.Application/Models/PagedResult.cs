/// <summary>
/// [MÔ HÌNH: PHÂN TRANG / PAGINATION WRAPPER]
/// Chức năng: Đóng gói danh sách dữ liệu (List) kèm theo siêu dữ liệu (metadata) phân trang.
/// 
/// Cách hoạt động:
/// - Chứa danh sách dữ liệu thực tế (Items) và thông số yêu cầu (TotalCount, PageIndex, PageSize).
/// - Tự động tính toán các thông số phụ trợ một cách thông minh (TotalPages, HasPreviousPage, HasNextPage) dựa vào số liệu thực tế.
/// - Sử dụng thuộc tính 'init' để đảm bảo an toàn thread (thread-safe) và tính bất biến sau khi khởi tạo.
/// 
/// Sử dụng: Dùng làm kiểu dữ liệu trả về cho các Query lấy danh sách dạng Result<PagedResult<ProductDto>>. 
/// Rất khớp với các UI Component dạng DataGrid hoặc Table ở Frontend Vue.js.
/// </summary>

namespace Shared.Application.Models;

public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PagedResult(List<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}