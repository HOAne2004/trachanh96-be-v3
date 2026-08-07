using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;

namespace Identity.Domain.Entities;

public class UserSession : AuditableEntity<Guid>
{
    public Guid UserId { get; private set; }
    public string RefreshTokenHash { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string DeviceName { get; private set; }
    public string IpAddress { get; private set; }

    protected UserSession()
    {
        RefreshTokenHash = null!;
        DeviceName = null!;
        IpAddress = null!;
    }

    internal UserSession(Guid userId, string refreshTokenHash, DateTime expiryDate, string deviceName, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
            throw new DomainException("RefreshTokenHash không được để trống.");

        if (expiryDate <= DateTime.UtcNow)
            throw new DomainException("Thời gian hết hạn phiên phải ở tương lai.");

        if (string.IsNullOrWhiteSpace(deviceName))
            throw new DomainException("Tên thiết bị không được để trống.");

        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException("Địa chỉ IP không được để trống.");

        Id = Guid.CreateVersion7();
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        ExpiryDate = expiryDate;
        DeviceName = deviceName;
        IpAddress = ipAddress;
        IsRevoked = false;
    }

    internal void Revoke()
    {
        if (IsRevoked) return;
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public bool IsValid() => !IsRevoked && ExpiryDate > DateTime.UtcNow;
}