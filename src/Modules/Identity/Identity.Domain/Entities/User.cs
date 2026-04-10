using Identity.Domain.Enums;
using Identity.Domain.ValueObjects;
using Shared.Domain.Interfaces;
using Shared.Domain.SeedWork;
using System.Linq;
using System.Security.Cryptography;

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

    // --- Audit & Soft Delete (Chỉ áp dụng cho User) ---
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

    public void ChangeEmail(string newEmail)
    {
        Email = EmailAddress.Create(newEmail);
        EmailVerified = false;
    }

    // ĐÃ SỬA: Xóa các check IsDeleted của Address
    public void AddAddress(string name, string rawPhone, string detail, string province, string district, string commune, double? lat, double? lng, bool isDefault)
    {
        if (_addresses.Count >= 5)
            throw new InvalidOperationException("Không thể thêm quá 5 địa chỉ.");

        if (_addresses.Count == 0)
        {
            isDefault = true;
        }
        else if (isDefault)
        {
            foreach (var addr in _addresses) addr.RemoveDefault();
        }

        var newAddress = new Address(name, rawPhone, detail, province, district, commune, lat, lng, isDefault);
        _addresses.Add(newAddress);
    }

    public void RestoreAccount()
    {
        if (!IsDeleted) throw new InvalidOperationException("Tài khoản chưa bị xóa.");

        IsDeleted = false;
        DeletedAt = null;
        Status = UserStatusEnum.Active;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public void AnonymizeEmailForHardDelete()
    {
        if (!IsDeleted) throw new InvalidOperationException("Chỉ được vô danh hóa tài khoản đã xóa mềm.");
        Email = EmailAddress.Create($"deleted_{Guid.NewGuid():N}@anonymized.com");
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("PasswordHash không hợp lệ.");
        PasswordHash = newPasswordHash;
    }

    public void UpdateProfile(string fullName, string? rawPhone, string? thumbnailUrl)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
            throw new ArgumentException("Họ tên không hợp lệ hoặc quá dài.");

        FullName = fullName.Trim();
        ThumbnailUrl = thumbnailUrl?.Trim();

        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            Phone = null;
        }
        else
        {
            Phone = PhoneNumber.Create(rawPhone);
        }
    }
    public void LockAccount(DateTime lockoutEndTime)
    {
        if (lockoutEndTime <= DateTime.UtcNow)
            throw new ArgumentException("Thời gian khóa phải ở tương lai.");

        Status = UserStatusEnum.Locked;
        LockoutEnd = lockoutEndTime;
    }

    public void UpdateAddress(int addressId, string name, string rawPhone, string detail, string province, string district, string commune, double? lat, double? lng, bool isDefault)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new InvalidOperationException("Không tìm thấy địa chỉ hợp lệ.");

        address.Update(name, rawPhone, detail, province, district, commune, lat, lng);

        if (isDefault && !address.IsDefault)
        {
            foreach (var addr in _addresses) addr.RemoveDefault();
            address.SetAsDefault();
        }
    }

    public void RemoveAddress(int addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new InvalidOperationException("Không tìm thấy địa chỉ hợp lệ.");

        if (address.IsDefault)
        {
            var fallbackAddress = _addresses.FirstOrDefault(a => a.Id != addressId);
            if (fallbackAddress != null)
            {
                fallbackAddress.SetAsDefault();
            }
        }

        _addresses.Remove(address);
    }

    public string GeneratePasswordResetToken(double expiryMinutes = 15)
    {
        var token = GenerateSecureOtp();
        ResetPasswordToken = token;
        ResetPasswordTokenExpiryTime = DateTime.UtcNow.AddMinutes(expiryMinutes);
        return token;
    }

    public void ConsumePasswordResetToken(string token, string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(ResetPasswordToken) || ResetPasswordToken != token)
            throw new InvalidOperationException("Mã xác thực không chính xác.");

        if (ResetPasswordTokenExpiryTime < DateTime.UtcNow)
            throw new InvalidOperationException("Mã xác thực đã hết hạn.");

        PasswordHash = newPasswordHash;
        ResetPasswordToken = null;
        ResetPasswordTokenExpiryTime = null;
    }

    public string GenerateEmailVerificationToken(double expiryHours = 24)
    {
        var token = GenerateSecureOtp();
        VerificationToken = token;
        VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
        return token;
    }

    public void VerifyEmail(string token)
    {
        if (EmailVerified)
            throw new InvalidOperationException("Tài khoản này đã được xác thực từ trước.");

        if (string.IsNullOrWhiteSpace(VerificationToken) || VerificationToken != token)
            throw new InvalidOperationException("Mã xác thực không chính xác.");

        if (VerificationTokenExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại.");

        EmailVerified = true;
        VerificationToken = null;
        VerificationTokenExpiresAt = null;
    }

    private static string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    // Logic: Admin thay đổi quyền của User
    public void ChangeRole(UserRoleEnum newRole)
    {
        Role = newRole;
    }

    // Logic: Admin mở khóa tài khoản trước thời hạn
    public void UnlockAccount()
    {
        if (Status != UserStatusEnum.Locked)
            throw new InvalidOperationException("Tài khoản này hiện không bị khóa.");

        Status = UserStatusEnum.Active;
        LockoutEnd = null;
        FailedLoginAttempts = 0; // Reset luôn số đếm để user đăng nhập lại từ đầu
    }
}