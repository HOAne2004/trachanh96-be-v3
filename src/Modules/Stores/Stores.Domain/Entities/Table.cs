using Shared.Domain.Interfaces;
using Shared.Domain.SeedWork;

namespace Stores.Domain.Entities;

public class Table : Entity<int>, ISoftDeletableEntity
{
    public int AreaId { get; private set; }
    public string Name { get; private set; }
    public int SeatCapacity { get; private set; }
    public string QrCodeToken { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    protected Table() { 
        Name = null!;
        SeatCapacity = 0!;
        QrCodeToken = null!;
    }

    internal Table(int areaId, string name, int seatCapacity)
    {
        ValidateName(name);
        ValidateSeatCapacity(seatCapacity);

        AreaId = areaId;
        Name = name.Trim();
        SeatCapacity = seatCapacity;

        IsActive = true;
        IsDeleted = false;
        QrCodeToken = GenerateQrToken();
    }

    internal void UpdateDetails(string name, int seatCapacity, bool isActive)
    {
        EnsureNotDeleted();
        ValidateName(name);
        ValidateSeatCapacity(seatCapacity);

        Name = name.Trim();
        SeatCapacity = seatCapacity;
        IsActive = isActive;
    }

    internal void Activate()
    {
        EnsureNotDeleted();
        IsActive = true;
    }

    internal void Deactivate()
    {
        EnsureNotDeleted();
        IsActive = false;
    }

    internal void RegenerateQrCode()
    {
        EnsureNotDeleted();
        QrCodeToken = GenerateQrToken();
    }

    internal void Delete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        IsActive = false;
        DeletedAt = DateTime.UtcNow; // Khôi phục DeletedAt
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên bàn không được để trống.");

        if (name.Length > 50)
            throw new ArgumentException("Tên bàn quá dài.");
    }

    private static void ValidateSeatCapacity(int seatCapacity)
    {
        if (seatCapacity <= 0)
            throw new ArgumentException("Sức chứa của bàn phải lớn hơn 0.");

        if (seatCapacity > 20)
            throw new ArgumentException("Sức chứa quá lớn đối với một bàn.");
    }

    private static string GenerateQrToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Không thể thao tác trên bàn đã bị xóa.");
    }
}