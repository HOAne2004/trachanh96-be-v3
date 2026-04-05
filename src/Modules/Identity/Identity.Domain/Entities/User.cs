using Identity.Domain.Enums;
using Identity.Domain.ValueObjects;
using Shared.Domain.Interfaces;
using Shared.Domain.SeedWork;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Identity.Domain.Entities;

public class User : AggregateRoot<int>, IAuditableEntity, ISoftDeletableEntity
{
    public Guid PublicId { get; private set; }
    public EmailAddress Email { get; private set; }
    public string FullName { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRoleEnum Role { get; private set; }
    public UserStatusEnum Status { get; private set; }

    // --- Verification & Security ---
    public bool EmailVerified { get; private set; }
    public string? VerificationToken { get; private set; }
    public DateTime? VerificationTokenExpiresAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }

    // --- Auth Tokens ---
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public string? ResetPasswordToken { get; private set; }
    public DateTime? ResetPasswordTokenExpiryTime { get; private set; }

    // --- Audit & Soft Delete (Tự động quản lý bởi Interceptor) ---
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // --- Navigation  ---
    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    protected User()
    {
        Email = null!;
        FullName = null!;
        PasswordHash = null!;
    }
    // 1. Constructor
    public User(string email, string fullName, string passwordHash, string? rawPhone = null, UserRoleEnum role = UserRoleEnum.Customer)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
            throw new ArgumentException("Họ tên không hợp lệ hoặc quá dài.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash không được để trống.");

        PublicId = Guid.NewGuid();
        Email = EmailAddress.Create(email);
        FullName = fullName.Trim();
        PasswordHash = passwordHash;
        Role = role;
        Status = UserStatusEnum.Active;
        EmailVerified = false;

        if (!string.IsNullOrWhiteSpace(rawPhone))
        {
            Phone = PhoneNumber.Create(rawPhone);
        }
    }

    // 2. Logic: Đổi Email (Trigger xác thực lại)
    public void ChangeEmail(string newEmail)
    {
        Email = EmailAddress.Create(newEmail);
        EmailVerified = false;
    }

    // 3. Logic: Thêm địa chỉ (Giới hạn số lượng & check default edge case)
    public void AddAddress(string name, string rawPhone, string detail, string province, string district, string commune, double? lat, double? lng, bool isDefault)
    {
        var activeAddresses = _addresses.Where(a => !a.IsDeleted).ToList();

        if (activeAddresses.Count >= 5)
            throw new InvalidOperationException("Không thể thêm quá 5 địa chỉ.");

        if (activeAddresses.Count == 0)
        {
            isDefault = true;
        }
        else if (isDefault)
        {
            foreach (var addr in activeAddresses) addr.RemoveDefault();
        }

        var newAddress = new Address(name, rawPhone, detail, province, district, commune, lat, lng, isDefault);
        _addresses.Add(newAddress);
    }

    // 4. Logic: Khôi phục tài khoản (Reset hoàn toàn trạng thái bảo mật)
    public void RestoreAccount()
    {
        if (!IsDeleted) throw new InvalidOperationException("Tài khoản chưa bị xóa.");

        IsDeleted = false;
        DeletedAt = null;
        Status = UserStatusEnum.Active;

        // Reset security state
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    // 5. Logic: Vô danh hóa (Dùng GUID chống collision)
    public void AnonymizeEmailForHardDelete()
    {
        if (!IsDeleted) throw new InvalidOperationException("Chỉ được vô danh hóa tài khoản đã xóa mềm.");

        Email = EmailAddress.Create($"deleted_{Guid.NewGuid():N}@anonymized.com");
    }

    // 6. Logic: Đổi mật khẩu
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("PasswordHash không hợp lệ.");

        PasswordHash = newPasswordHash;
    }

    // 7. Logic: Khóa tài khoản
    public void LockAccount(DateTime lockoutEndTime)
    {
        if (lockoutEndTime <= DateTime.UtcNow)
            throw new ArgumentException("Thời gian khóa phải ở tương lai.");

        Status = UserStatusEnum.Locked;
        LockoutEnd = lockoutEndTime;
    }

    // 8. Cập nhật địa chỉ
    public void UpdateAddress(int addressId, string name, string rawPhone, string detail, string province, string district, string commune, double? lat, double? lng, bool isDefault)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId && !a.IsDeleted)
            ?? throw new InvalidOperationException("Không tìm thấy địa chỉ hợp lệ.");

        address.Update(name, rawPhone, detail, province, district, commune, lat, lng);

        // Xử lý logic đổi default
        if (isDefault && !address.IsDefault)
        {
            var activeAddresses = _addresses.Where(a => !a.IsDeleted);
            foreach (var addr in activeAddresses) addr.RemoveDefault();

            address.SetAsDefault();
        }
    }

    // 9. Xóa mềm địa chỉ và Auto-promote
    public void RemoveAddress(int addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId && !a.IsDeleted)
            ?? throw new InvalidOperationException("Không tìm thấy địa chỉ hợp lệ.");

        address.MarkAsDeleted();

        if (address.IsDefault)
        {
            address.RemoveDefault();

            var fallbackAddress = _addresses.FirstOrDefault(a => !a.IsDeleted);
            if (fallbackAddress != null)
            {
                fallbackAddress.SetAsDefault();
            }
        }
    }

    // 10. Logic: Tạo Token khôi phục mật khẩu
    public string GeneratePasswordResetToken(double expiryMinutes = 15)
    {
        var token = GenerateSecureOtp();
        ResetPasswordToken = token;
        ResetPasswordTokenExpiryTime = DateTime.UtcNow.AddMinutes(expiryMinutes);
        return token;
    }

    // 11. Logic: Xác nhận Token và Đổi mật khẩu
    public void ConsumePasswordResetToken(string token, string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(ResetPasswordToken) || ResetPasswordToken != token)
            throw new InvalidOperationException("Mã xác thực không chính xác.");

        if (ResetPasswordTokenExpiryTime < DateTime.UtcNow)
            throw new InvalidOperationException("Mã xác thực đã hết hạn.");

        // Đổi mật khẩu
        PasswordHash = newPasswordHash;

        // Hủy token sau khi dùng xong để tránh bị dùng lại (Replay Attack)
        ResetPasswordToken = null;
        ResetPasswordTokenExpiryTime = null;
    }

    // 12. Logic: Tạo Token xác thực Email (Sống 24 giờ)
    public string GenerateEmailVerificationToken(double expiryHours = 24)
    {
        var token = GenerateSecureOtp();
        VerificationToken = token;
        VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
        return token;
    }

    // 13. Logic: Kiểm tra Token và Kích hoạt Email
    public void VerifyEmail(string token)
    {
        if (EmailVerified)
            throw new InvalidOperationException("Tài khoản này đã được xác thực từ trước.");

        if (string.IsNullOrWhiteSpace(VerificationToken) || VerificationToken != token)
            throw new InvalidOperationException("Mã xác thực không chính xác.");

        if (VerificationTokenExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại.");

        EmailVerified = true;

        // Hủy token sau khi dùng
        VerificationToken = null;
        VerificationTokenExpiresAt = null;
    }

    private static string GenerateSecureOtp()
    {
        // Sinh ra 1 số ngẫu nhiên chuẩn mật mã học từ 100000 đến 999999
        var randomNumber = RandomNumberGenerator.GetInt32(100000, 1000000);
        return randomNumber.ToString();
    }
}