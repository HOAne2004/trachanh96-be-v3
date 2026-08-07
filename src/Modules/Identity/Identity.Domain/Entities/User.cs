using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;
using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;

namespace Identity.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    #region [ Constants ]
    private const int MaxFailedLoginAttempts = 5;
    private const int DefaultLockoutMinutes = 15;
    private const int MaxAddressesPerUser = 5;
    private const int MaxFullNameLength = 150;
    #endregion

    #region [ Properties ]
    public EmailAddress Email { get; private set; }
    public string FullName { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string PasswordHash { get; private set; }
    public UserStatusEnum Status { get; private set; }

    public bool EmailVerified { get; private set; }
    public string? VerificationToken { get; private set; }
    public DateTime? VerificationTokenExpiresAt { get; private set; }

    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public Guid SecurityStamp { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    #endregion

    #region [ Navigation ]
    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    private readonly List<UserSession> _sessions = new();
    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    #endregion

    protected User()
    {
        Email = null!;
        FullName = null!;
        PasswordHash = null!;
    }

    public User(string email, string fullName, string passwordHash, string? rawPhone = null)
    {
        Id = Guid.CreateVersion7();
        Email = EmailAddress.Create(email);
        SetFullName(fullName);
        PasswordHash = passwordHash;
        Status = UserStatusEnum.Active;
        SecurityStamp = Guid.NewGuid();

        if (!string.IsNullOrWhiteSpace(rawPhone))
            Phone = PhoneNumber.Create(rawPhone);
    }

    #region [ Security & Authentication Behaviors ]
    public void IncreaseFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            Status = UserStatusEnum.Locked;
            LockoutEnd = DateTime.UtcNow.AddMinutes(DefaultLockoutMinutes);
            RevokeAllSessions(); // Tự động văng mọi thiết bị khi bị khóa
            UpdateSecurityStamp();
            AddDomainEvent(new UserAccountLockedEvent(Id, LockoutEnd.Value));
        }
    }

    public void UpdateSecurityStamp()
    {
        SecurityStamp = Guid.NewGuid();
    }

    public void ResetFailedLogin()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        if (Status == UserStatusEnum.Locked) Status = UserStatusEnum.Active;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("PasswordHash không hợp lệ.");
        PasswordHash = newPasswordHash;
    }

    public void LockAccount(DateTime lockoutEndTime)
    {
        if (lockoutEndTime <= DateTime.UtcNow)
            throw new DomainException("Thời gian khóa phải ở tương lai.");

        Status = UserStatusEnum.Locked;
        LockoutEnd = lockoutEndTime;
        RevokeAllSessions();
        UpdateSecurityStamp();
        AddDomainEvent(new UserAccountLockedEvent(Id, lockoutEndTime));
    }

    public void UnlockAccount()
    {
        if (Status != UserStatusEnum.Locked)
            throw new DomainException("Tài khoản này hiện không bị khóa.");

        Status = UserStatusEnum.Active;
        LockoutEnd = null;
        FailedLoginAttempts = 0;
    }

    // Tầng Application sẽ gọi Generate OTP và truyền vào hàm này
    public void SetVerificationToken(string token, double expiryHours = 24)
    {
        VerificationToken = token;
        VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
    }

    public void VerifyEmail(string token)
    {
        if (EmailVerified)
            throw new DomainException("Tài khoản này đã được xác thực từ trước.");

        if (string.IsNullOrWhiteSpace(VerificationToken) || VerificationToken != token)
            throw new DomainException("Mã xác thực không chính xác.");

        if (VerificationTokenExpiresAt < DateTime.UtcNow)
            throw new DomainException("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại.");

        EmailVerified = true;
        VerificationToken = null;
        VerificationTokenExpiresAt = null;
    }

    public void SetPasswordResetToken(string token, int expiryMinutes = 15)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
    }

    public void ResetPassword(string token, string newPasswordHash)
    {
        // 1. Kiểm tra tính hợp lệ của Token
        if (string.IsNullOrWhiteSpace(PasswordResetToken) || PasswordResetToken != token)
            throw new DomainException("Mã xác thực không chính xác.");

        if (PasswordResetTokenExpiresAt < DateTime.UtcNow)
            throw new DomainException("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại.");

        // 2. Đổi mật khẩu
        ChangePassword(newPasswordHash);

        // 3. Xóa Token để tránh dùng lại (One-time use)
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;

        // 4. BẢO MẬT TỐI CAO: Khóa mõm tất cả các thiết bị đang đăng nhập!
        RevokeAllSessions();
        UpdateSecurityStamp(); // JWT hiện tại sẽ chết ngay lập tức
        ResetFailedLogin();    // Reset đếm sai (nếu có)

        AddDomainEvent(new UserPasswordResetEvent(Id));
    }
    #endregion

    #region [ Session Behaviors ]
    public UserSession AddSession(string refreshTokenHash, DateTime expiryDate, string deviceName, string ipAddress)
    {
        var session = new UserSession(Id, refreshTokenHash, expiryDate, deviceName, ipAddress);
        _sessions.Add(session);
        return session;
    }

    public void RevokeSession(string refreshTokenHash)
    {
        var session = _sessions.FirstOrDefault(s => s.RefreshTokenHash == refreshTokenHash);
        if (session == null) throw new DomainException("Không tìm thấy phiên đăng nhập.");
        session.Revoke();
    }

    public void RevokeSessionById(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
            throw new DomainException("Không tìm thấy phiên đăng nhập.");

        session.Revoke();
    }

    public void RevokeAllSessions()
    {
        foreach (var session in _sessions.Where(s => s.IsValid()))
            session.Revoke();
    }

    // Bổ sung lại hàm kiểm tra tính hợp lệ của RefreshToken
    public bool IsRefreshTokenValid(string refreshTokenHash)
    {
        var session = _sessions.FirstOrDefault(s => s.RefreshTokenHash == refreshTokenHash);
        return session != null && session.IsValid();
    }

    #endregion

    #region [ Profile Behaviors ]
    public void AssignRole(Guid roleId)
    {
        if (!_userRoles.Any(ur => ur.RoleId == roleId))
        {
            _userRoles.Add(new UserRole(Id, roleId));
        }
    }

    public void UpdateProfile(string fullName, string? rawPhone, string? thumbnailUrl)
    {
        SetFullName(fullName);

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
            ThumbnailUrl = thumbnailUrl.Trim();

        if (string.IsNullOrWhiteSpace(rawPhone))
            Phone = null;
        else
            Phone = PhoneNumber.Create(rawPhone);
    }

    public void AdminUpdateUser(string fullName, string email, string? rawPhone)
    {
        SetFullName(fullName);

        if (string.IsNullOrWhiteSpace(rawPhone))
            Phone = null;
        else
            Phone = PhoneNumber.Create(rawPhone);

        // Kiểm tra xem Admin có đổi Email không
        var newEmail = EmailAddress.Create(email);
        if (Email.Value != newEmail.Value)
        {
            var oldEmail = Email.Value;
            Email = newEmail;
            // BẢO MẬT: Admin thao tác nên tự động xác thực email mới
            EmailVerified = true;
            VerificationToken = null;
            VerificationTokenExpiresAt = null;

            // Văng thiết bị cũ để bắt user đăng nhập lại bằng email mới cho an toàn
            RevokeAllSessions();
            UpdateSecurityStamp();

            AddDomainEvent(new UserEmailChangedEvent(Id, oldEmail, newEmail.Value));
        }
    }

    public void ChangeEmail(string newEmail)
    {
        var email = EmailAddress.Create(newEmail);
        if (Email.Value == email.Value) return;

        var oldEmail = Email.Value;
        Email = email;
        EmailVerified = false;

        // BẢO MẬT: nếu attacker chiếm được session hiện tại và tự đổi email,
        // phải buộc đăng xuất toàn bộ thiết bị để chủ tài khoản phát hiện bất thường.
        RevokeAllSessions();
        UpdateSecurityStamp();

        AddDomainEvent(new UserEmailChangedEvent(Id, oldEmail, email.Value));
    }

    public void RestoreAccount()
    {
        if (!IsDeleted) throw new DomainException("Tài khoản chưa bị xóa.");

        IsDeleted = false;
        DeletedAt = null;
        Status = UserStatusEnum.Active;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public void AnonymizeEmailForHardDelete()
    {
        if (!IsDeleted) throw new DomainException("Chỉ được vô danh hóa tài khoản đã xóa mềm.");

        var anonymizedId = Guid.CreateVersion7();

        Email = EmailAddress.Create($"deleted_{anonymizedId:N}@anonymized.com");
        FullName = "Deleted User";
        Phone = null;
        ThumbnailUrl = null;
    }

    public void SyncRoles(IEnumerable<Guid> roleIds)
    {
        // Xóa các role cũ
        _userRoles.Clear();

        // Thêm các role mới
        foreach (var roleId in roleIds.Distinct())
        {
            _userRoles.Add(new UserRole(Id, roleId));
        }
    }

    // Dành cho Admin chủ động xác thực Email khi tạo tài khoản nội bộ
    public void VerifyEmailByAdmin()
    {
        EmailVerified = true;
        VerificationToken = null;
        VerificationTokenExpiresAt = null;
    }

    // Xóa mềm tài khoản + ngắt toàn bộ Session + đổi Security Stamp
    public void DeleteAccount()
    {
        if (IsDeleted)
            throw new DomainException("Tài khoản này đã bị xóa từ trước.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Status = UserStatusEnum.Inactive;

        // BẢO MẬT: Ngắt lập tức toàn bộ phiên làm việc của tài khoản bị xóa
        RevokeAllSessions();
        UpdateSecurityStamp();

        AddDomainEvent(new UserAccountDeletedEvent(Id));
    }

    private void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > MaxFullNameLength)
            throw new DomainException("Họ tên không hợp lệ hoặc quá dài.");

        FullName = fullName.Trim();
    }
    #endregion

    #region [ Address Behaviors ]
    public Address AddAddress(string name, string rawPhone, string detail, string province, string district, string commune, double? lat, double? lng, bool isDefault)
    {
        if (_addresses.Count >= MaxAddressesPerUser)
            throw new DomainException($"Không thể thêm quá {MaxAddressesPerUser} địa chỉ.");

        var address = new Address(name, rawPhone, detail, province, district, commune, lat, lng, isDefault);

        if (_addresses.Count == 0)
        {
            address.SetAsDefault();
        }
        else if (address.IsDefault)
        {
            foreach (var addr in _addresses) addr.RemoveDefault();
        }

        _addresses.Add(address);
        return address;
    }

    public void UpdateAddress(Guid addressId, string name, string rawPhone, string detail, string province, string district, string commune, double? lat, double? lng, bool isDefault)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new DomainException("Không tìm thấy địa chỉ hợp lệ.");

        address.Update(name, rawPhone, detail, province, district, commune, lat, lng);

        if (isDefault && !address.IsDefault)
        {
            foreach (var addr in _addresses) addr.RemoveDefault();
            address.SetAsDefault();
        }
    }

    public void RemoveAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new DomainException("Không tìm thấy địa chỉ hợp lệ.");

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

    public void SetDefaultAddress(Guid addressId)
    {
        var targetAddress = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (targetAddress == null)
            throw new DomainException("Không tìm thấy địa chỉ yêu cầu.");

        if (targetAddress.IsDefault)
            return;

        // Gỡ cờ mặc định của các địa chỉ khác
        foreach (var address in _addresses)
        {
            address.RemoveDefault(); // Gọi hàm RemoveDefault() đã có sẵn trong Address.cs
        }

        // Đặt địa chỉ mục tiêu làm mặc định
        targetAddress.SetAsDefault();
    }
    #endregion
}