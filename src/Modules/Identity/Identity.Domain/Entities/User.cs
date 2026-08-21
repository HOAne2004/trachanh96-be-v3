using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;
using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    #region [ Constants ]
    private const int MaxFailedLoginAttempts = 5;
    private const int DefaultLockoutMinutes = 15;
    private const int MaxAddressesPerUser = 5;
    private const int MaxFullNameLength = 150;
    private const int MaxPasswordResetAttempts = 5;
    private const int MaxEmailVerificationAttempts = 5;
    #endregion

    #region [ Properties ]
    public EmailAddress Email { get; private set; }

    /// <summary>Email mới đang chờ xác nhận OTP (mô hình 2 bước) - null nếu không có yêu cầu nào đang chờ.</summary>
    public string? PendingEmail { get; private set; }

    public string FullName { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string PasswordHash { get; private set; }
    public UserStatusEnum Status { get; private set; }

    public bool EmailVerified { get; private set; }
    public string? VerificationToken { get; private set; }
    public DateTime? VerificationTokenExpiresAt { get; private set; }
    public int EmailVerificationAttempts { get; private set; }

    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public Guid SecurityStamp { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    public int PasswordResetAttempts { get; private set; }
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
            RevokeAllSessions();
            UpdateSecurityStamp();
            AddDomainEvent(new UserAccountLockedEvent(Id, LockoutEnd.Value, "Đăng nhập sai quá số lần cho phép."));
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
        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    public void LockAccount(DateTime lockoutEndTime, string reason)
    {
        if (lockoutEndTime <= DateTime.UtcNow)
            throw new DomainException("Thời gian khóa phải ở tương lai.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do khóa tài khoản không được để trống.");

        Status = UserStatusEnum.Locked;
        LockoutEnd = lockoutEndTime;
        RevokeAllSessions();
        UpdateSecurityStamp();
        AddDomainEvent(new UserAccountLockedEvent(Id, lockoutEndTime, reason));
    }

    public void UnlockAccount()
    {
        if (Status != UserStatusEnum.Locked)
            throw new DomainException("Tài khoản này hiện không bị khóa.");

        Status = UserStatusEnum.Active;
        LockoutEnd = null;
        FailedLoginAttempts = 0;
    }

    public void SetVerificationToken(string token, double expiryHours = 24)
    {
        VerificationToken = token;
        VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
        EmailVerificationAttempts = 0;
    }

    public void VerifyEmail(string token)
    {
        if (EmailVerified)
            throw new DomainException("Tài khoản này đã được xác thực từ trước.");

        if (string.IsNullOrWhiteSpace(VerificationToken))
            throw new DomainException("Mã xác thực không chính xác.");

        if (VerificationTokenExpiresAt < DateTime.UtcNow)
        {
            ClearVerificationState();
            throw new DomainException("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại.");
        }

        if (VerificationToken != token)
        {
            EmailVerificationAttempts++;
            if (EmailVerificationAttempts >= MaxEmailVerificationAttempts)
            {
                ClearVerificationState();
                throw new DomainException("Bạn đã nhập sai mã quá số lần cho phép. Vui lòng yêu cầu gửi lại mã mới.");
            }
            throw new DomainException("Mã xác thực không chính xác.");
        }

        EmailVerified = true;
        ClearVerificationState();
    }

    public void SetPasswordResetToken(string token, int expiryMinutes = 15, bool raiseEvent = true)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        PasswordResetAttempts = 0;

        if (raiseEvent)
        {
            AddDomainEvent(new PasswordResetRequestedEvent(Id, Email.Value, FullName, token));
        }
    }

    public void ResetPassword(string token, string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(PasswordResetToken))
            throw new DomainException("Mã xác thực không chính xác.");

        if (PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            ClearPasswordResetState();
            throw new DomainException("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại.");
        }

        if (PasswordResetToken != token)
        {
            PasswordResetAttempts++;
            if (PasswordResetAttempts >= MaxPasswordResetAttempts)
            {
                ClearPasswordResetState();
                throw new DomainException("Bạn đã nhập sai mã quá số lần cho phép. Vui lòng yêu cầu gửi lại mã mới.");
            }
            throw new DomainException("Mã xác thực không chính xác.");
        }

        ChangePassword(newPasswordHash);
        ClearPasswordResetState();

        RevokeAllSessions();
        UpdateSecurityStamp();
        ResetFailedLogin();

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

        var newEmail = EmailAddress.Create(email);
        if (Email.Value != newEmail.Value)
        {
            var oldEmail = Email.Value;
            Email = newEmail;
            EmailVerified = true;
            // Admin đổi email trực tiếp -> hủy luôn mọi yêu cầu đổi email self-service đang chờ
            // (nếu có), tránh xung đột giữa 2 nguồn thay đổi email cùng lúc.
            ClearVerificationState();

            RevokeAllSessions();
            UpdateSecurityStamp();

            AddDomainEvent(new UserEmailChangedEvent(Id, oldEmail, newEmail.Value));
        }
    }

    /// <summary>
    /// Bước 1/2 của luồng đổi Email tự-phục-vụ: CHỈ lưu email mới vào PendingEmail và sinh OTP -
    /// KHÔNG đụng đến Email/EmailVerified/Session/SecurityStamp hiện tại. Nếu người dùng gõ nhầm
    /// email mới, tài khoản vẫn nguyên vẹn, vẫn đăng nhập bình thường bằng email cũ - chỉ cần gọi
    /// lại hàm này với địa chỉ đúng. Email chỉ thực sự đổi khi ConfirmEmailChange() xác nhận đúng OTP.
    /// </summary>
    public void RequestEmailChange(string newEmail, string token, double expiryHours = 24)
    {
        var normalizedNewEmail = EmailAddress.Create(newEmail).Value;

        if (normalizedNewEmail == Email.Value)
            throw new DomainException("Email mới phải khác với Email hiện tại.");

        PendingEmail = normalizedNewEmail;
        VerificationToken = token;
        VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
        EmailVerificationAttempts = 0;

        AddDomainEvent(new ChangeEmailRequestedEvent(Id, normalizedNewEmail, FullName, token));
    }

    /// <summary>Raise event gửi email mời thiết lập mật khẩu - tách riêng khỏi MarkCreatedByAdmin
    /// (chỉ phục vụ audit log) để không lẫn 2 mục đích khác nhau vào cùng 1 event.</summary>
    public void RequestAccountInvitationEmail(string invitationToken)
    {
        AddDomainEvent(new AccountInvitationRequestedEvent(Id, Email.Value, FullName, invitationToken));
    }

    /// <summary>
    /// Bước 2/2: xác nhận OTP gửi tới PendingEmail. Chỉ khi đúng, Email mới mới thực sự được
    /// áp dụng - đây mới là thời điểm cần Revoke session/đổi SecurityStamp, vì định danh đăng
    /// nhập (Email) thực sự vừa thay đổi.
    /// </summary>
    public void ConfirmEmailChange(string token)
    {
        if (string.IsNullOrWhiteSpace(PendingEmail))
            throw new DomainException("Không có yêu cầu đổi email nào đang chờ xác nhận.");

        if (string.IsNullOrWhiteSpace(VerificationToken))
            throw new DomainException("Mã xác thực không chính xác.");

        if (VerificationTokenExpiresAt < DateTime.UtcNow)
        {
            ClearVerificationState();
            throw new DomainException("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại.");
        }

        if (VerificationToken != token)
        {
            EmailVerificationAttempts++;
            if (EmailVerificationAttempts >= MaxEmailVerificationAttempts)
            {
                ClearVerificationState();
                throw new DomainException("Bạn đã nhập sai mã quá số lần cho phép. Vui lòng yêu cầu gửi lại mã mới.");
            }
            throw new DomainException("Mã xác thực không chính xác.");
        }

        var oldEmail = Email.Value;
        Email = EmailAddress.Create(PendingEmail);
        EmailVerified = true;

        ClearVerificationState();

        RevokeAllSessions();
        UpdateSecurityStamp();

        AddDomainEvent(new UserEmailChangedEvent(Id, oldEmail, Email.Value));
    }

    /// <summary>Hủy yêu cầu đổi email đang chờ xác nhận (người dùng đổi ý, hoặc Admin can thiệp).</summary>
    public void CancelEmailChangeRequest()
    {
        if (string.IsNullOrWhiteSpace(PendingEmail))
            throw new DomainException("Không có yêu cầu đổi email nào đang chờ xác nhận.");

        ClearVerificationState();
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
        PendingEmail = null;
    }

    public void SyncRoles(IEnumerable<Guid> roleIds)
    {
        var newRoleIds = roleIds.Distinct().ToList();
        var oldRoleIds = _userRoles.Select(ur => ur.RoleId).ToList();

        _userRoles.Clear();
        foreach (var roleId in newRoleIds)
        {
            _userRoles.Add(new UserRole(Id, roleId));
        }

        if (!oldRoleIds.OrderBy(x => x).SequenceEqual(newRoleIds.OrderBy(x => x)))
        {
            AddDomainEvent(new UserRolesChangedEvent(Id, oldRoleIds, newRoleIds));
        }
    }

    public void VerifyEmailByAdmin()
    {
        EmailVerified = true;
        ClearVerificationState();
    }

    public void DeleteAccount()
    {
        if (IsDeleted)
            throw new DomainException("Tài khoản này đã bị xóa từ trước.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Status = UserStatusEnum.Inactive;

        RevokeAllSessions();
        UpdateSecurityStamp();

        AddDomainEvent(new UserAccountDeletedEvent(Id));
    }

    public void MarkCreatedByAdmin(IEnumerable<Guid> assignedRoleIds)
    {
        AddDomainEvent(new UserCreatedByAdminEvent(Id, assignedRoleIds.ToList()));
    }

    [MemberNotNull(nameof(FullName))]
    private void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > MaxFullNameLength)
            throw new DomainException("Họ tên không hợp lệ hoặc quá dài.");

        FullName = fullName.Trim();
    }

    private void ClearVerificationState()
    {
        PendingEmail = null;
        VerificationToken = null;
        VerificationTokenExpiresAt = null;
        EmailVerificationAttempts = 0;
    }

    private void ClearPasswordResetState()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
        PasswordResetAttempts = 0;
    }
    #endregion

    #region [ Address Behaviors ]
    public Address AddAddress(string name, string rawPhone, string detail, string province, string? district, string commune, double? lat, double? lng, bool isDefault)
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

    public void UpdateAddress(Guid addressId, string name, string rawPhone, string detail, string province, string? district, string commune, double? lat, double? lng, bool isDefault)
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

        foreach (var address in _addresses)
        {
            address.RemoveDefault();
        }

        targetAddress.SetAsDefault();
    }
    #endregion
}