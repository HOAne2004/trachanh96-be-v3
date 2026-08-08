namespace Identity.Application.DTOs.Response;

public record UserDetailsAdminDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string? ThumbnailUrl,
    IReadOnlyList<string> Roles,
    string Status,
    bool EmailVerified,
    int FailedLoginAttempts,
    DateTime? LockoutEnd,
    DateTime CreatedAt,

    // Bổ sung: thông tin từ các field đã thêm vào User.cs qua các lượt review gần đây -
    // hữu ích khi Admin cần hỗ trợ người dùng gặp sự cố (VD: quên đã yêu cầu đổi email,
    // hoặc đang bị khóa do nhập sai OTP quá nhiều lần).
    string? PendingEmail,
    int PasswordResetAttempts,
    int EmailVerificationAttempts
);