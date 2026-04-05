using Shared.Domain.Interfaces;
using Shared.Domain.SeedWork;

namespace Stores.Domain.Entities;

public class Area : Entity<int>, ISoftDeletableEntity
{
    public int StoreId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    private readonly List<Table> _tables = new();
    public IReadOnlyCollection<Table> Tables => _tables;

    protected Area()
    {
        Name = null!;

    }

    internal Area(int storeId, string name)
    {
        ValidateName(name);

        StoreId = storeId;
        Name = name.Trim();
        IsActive = true;
        IsDeleted = false;
    }

    internal void UpdateDetails(string name, bool isActive)
    {
        ValidateName(name);

        Name = name.Trim();
        IsActive = isActive;
    }

    internal Table AddTable(string name, int seatCapacity)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Không thể thêm bàn vào khu vực đã bị xóa.");

        if (!IsActive)
            throw new InvalidOperationException("Không thể thêm bàn vào khu vực đang ngừng hoạt động.");

        ValidateName(name);

        if (seatCapacity <= 0)
            throw new ArgumentException("Sức chứa của bàn phải lớn hơn 0.");

        var normalized = Normalize(name);

        if (_tables.Any(t => Normalize(t.Name) == normalized && !t.IsDeleted))
            throw new InvalidOperationException($"Bàn '{name}' đã tồn tại trong khu vực này.");

        var table = new Table(this.Id, name.Trim(), seatCapacity);
        _tables.Add(table);

        return table;
    }

    internal void RemoveTable(int tableId)
    {
        var table = _tables.FirstOrDefault(t => t.Id == tableId);

        if (table == null)
            throw new InvalidOperationException("Không tìm thấy bàn.");

        table.Delete();
    }

    internal void Delete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        IsActive = false;
        DeletedAt = DateTime.UtcNow; // Khôi phục DeletedAt

        // Xóa khu vực thì tự động xóa mềm luôn các bàn bên trong
        foreach (var table in _tables)
        {
            table.Delete();
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên khu vực không được để trống.");

        if (name.Length > 100)
            throw new ArgumentException("Tên khu vực quá dài.");
    }

    private static string Normalize(string name)
    {
        return name.Trim().ToLowerInvariant();
    }
}