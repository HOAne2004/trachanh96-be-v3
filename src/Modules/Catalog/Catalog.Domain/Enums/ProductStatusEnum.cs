namespace Catalog.Domain.Enums
{
    public enum ProductStatusEnum
    {
        Draft = 0,       // Đang soạn thảo, chưa có size/topping, không hiển thị
        Active = 1,      // Đang bán, hiển thị và cho phép đặt hàng
        Inactive = 2,    // Ngừng bán tạm thời (hết món), hiển thị mờ, không cho đặt
        Archived = 3,    // Lưu trữ vĩnh viễn, ẩn khỏi UI, giữ cho lịch sử
        ComingSoon = 4   // Sắp ra mắt, hiển thị UI marketing, không cho đặt
    }
}
