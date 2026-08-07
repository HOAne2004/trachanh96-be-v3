namespace Identity.Domain.Constants;

public static class IdentityPermissions
{
    // Nhóm quyền thao tác trên tài nguyên USER (Khách hàng, Nhân viên...)
    public static class Users
    {
        public const string View = "Identity.Users.View";       // Xem danh sách user
        public const string Create = "Identity.Users.Create";   // Tạo user mới (từ Admin)
        public const string Update = "Identity.Users.Update";   // Sửa thông tin user
        public const string Delete = "Identity.Users.Delete";   // Xóa mềm user
        public const string Lock = "Identity.Users.Lock";       // Khóa/Mở khóa tài khoản

        // Cấp Role cho User (Ví dụ: Thăng cấp 1 user thành Staff)
        public const string AssignRole = "Identity.Users.AssignRole";
    }

    // Nhóm quyền thao tác trên tài nguyên ROLE (Chức vụ & Quyền hạn)
    public static class Roles
    {
        public const string View = "Identity.Roles.View";       // Xem danh sách Role (Admin, Staff, Sales...)
        public const string Create = "Identity.Roles.Create";   // Tạo một Role mới tinh
        public const string Update = "Identity.Roles.Update";   // Sửa tên Role
        public const string Delete = "Identity.Roles.Delete";   // Xóa Role

        // Quản lý các Permission bên trong một Role (Màn hình check ma trận quyền)
        public const string ManagePermissions = "Identity.Roles.ManagePermissions";
        public const string AssignPermissions = "Identity.Roles.AssignPermissions"; 
    }

    // (Tùy chọn) Nhóm quyền thao tác trên PHIÊN ĐĂNG NHẬP
    public static class Sessions
    {
        public const string View = "Identity.Sessions.View";     // Xem user đang đăng nhập ở thiết bị nào
        public const string Revoke = "Identity.Sessions.Revoke"; // Cưỡng chế đăng xuất (Đá user ra)
    }

    // Nhóm quyền thao tác trên SỔ ĐỊA CHỈ của khách hàng
    public static class Addresses
    {
        // Cho phép xem địa chỉ của người khác, hoặc kéo data để làm thống kê vùng miền
        public const string View = "Identity.Addresses.View";

        // Cho phép nhân viên CSKH thêm địa chỉ giao hàng hộ khách
        public const string Create = "Identity.Addresses.Create";

        // Cho phép sửa địa chỉ hộ khách (vd: khách nhập sai số điện thoại nhận hàng)
        public const string Update = "Identity.Addresses.Update";

        // Cho phép xóa địa chỉ rác/spam của user
        public const string Delete = "Identity.Addresses.Delete";
    }
}