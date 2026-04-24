using Shared.Domain.Interfaces;
using Shared.Domain.SeedWork;
using Shared.Domain.ValueObjects;
using Stores.Domain.Enums;

namespace Stores.Domain.Entities;

// Record DTO dùng để truyền lịch hoạt động vào hàm SetOperatingHours
public record OperatingHourConfig(DayOfWeek Day, TimeSpan? Open, TimeSpan? Close, bool IsClosed);

public class Store : AggregateRoot<int>, IAuditableEntity, ISoftDeletableEntity
{
    private const string DEFAULT_CURRENCY = "VND";
    private const double DEFAULT_DELIVERY_RADIUS = 5.0;

    public Guid PublicId { get; private set; }
    public string StoreCode { get; private set; }
    public string Name { get; private set; }
    public Slug Slug { get; private set; }

    // UI Information
    public string? ImageUrl { get; private set; }
    public string? Description { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? WifiPassword { get; private set; }

    // Location
    public string FullAddress { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    // Delivery
    public double DeliveryRadiusKm { get; private set; }
    public Money BaseShippingFee { get; private set; }
    public Money ShippingFeePerKm { get; private set; }

    // Status
    public StoreStatusEnum Status { get; private set; }
    public DateTime? OpenDate { get; private set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation (Aggregate children)
    private readonly List<StoreOperatingHour> _operatingHours = new();
    public IReadOnlyCollection<StoreOperatingHour> OperatingHours => _operatingHours;

    private readonly List<Area> _areas = new();
    public IReadOnlyCollection<Area> Areas => _areas;

    protected Store()
    {
        Name = null!;
        Slug = null!;
        StoreCode = null!;
        FullAddress = null!;
        BaseShippingFee = null!;
        ShippingFeePerKm = null!;
    } // EF Core

    private Store(
        Guid publicId,
        string storeCode,
        string name,
        string fullAddress,
        double latitude,
        double longitude)
    {
        ValidateStoreCode(storeCode);
        ValidateName(name);
        ValidateAddress(fullAddress);
        ValidateGeo(latitude, longitude);

        PublicId = publicId;
        StoreCode = storeCode.Trim().ToUpper();
        Name = name.Trim();
        Slug = Slug.Create(Name);

        FullAddress = fullAddress.Trim();
        Latitude = latitude;
        Longitude = longitude;

        DeliveryRadiusKm = DEFAULT_DELIVERY_RADIUS;
        BaseShippingFee = Money.Create(15000, DEFAULT_CURRENCY);
        ShippingFeePerKm = Money.Create(5000, DEFAULT_CURRENCY);

        Status = StoreStatusEnum.Draft;
        IsDeleted = false;
    }

    public static Store Create(
        Guid publicId,
        string storeCode,
        string name,
        string address,
        double lat,
        double lng)
    {
        return new Store(publicId, storeCode, name, address, lat, lng);
    }

    // ---------------------------
    // BEHAVIORS
    // ---------------------------

    public void UpdateGeneralInfo(
        string name,
        string? description,
        string? imageUrl,
        string? phone,
        string? wifi)
    {
        ValidateName(name);

        Name = name.Trim();
        Slug = Slug.Create(Name); 
        Description = description;
        ImageUrl = imageUrl;
        PhoneNumber = phone;
        WifiPassword = wifi;
    }

    public void UpdateLocation(
        string address,
        double latitude,
        double longitude)
    {
        ValidateAddress(address);
        ValidateGeo(latitude, longitude);

        FullAddress = address.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }

    public void UpdateDeliveryPolicy(
        double radiusKm,
        Money baseFee,
        Money feePerKm)
    {
        if (radiusKm < 0)
            throw new ArgumentException("Bán kính giao hàng không thể âm.");

        if (baseFee.Currency != feePerKm.Currency)
            throw new InvalidOperationException("Loại tiền tệ không khớp.");

        DeliveryRadiusKm = radiusKm;
        BaseShippingFee = baseFee;
        ShippingFeePerKm = feePerKm;
    }

    // ---------------------------
    // STATE MACHINE
    // ---------------------------

    public void MarkAsComingSoon(DateTime? expectedOpenDate)
    {
        EnsureNotClosedPermanently();

        Status = StoreStatusEnum.ComingSoon;
        OpenDate = expectedOpenDate;
    }

    public void OpenStore()
    {
        EnsureNotClosedPermanently();

        if (!_operatingHours.Any())
            throw new InvalidOperationException(
                "Phải cấu hình giờ mở cửa trước khi cho phép cửa hàng hoạt động.");

        Status = StoreStatusEnum.Active;

        if (!OpenDate.HasValue)
            OpenDate = DateTime.UtcNow;
    }

    public void PauseOperations()
    {
        if (Status != StoreStatusEnum.Active)
            throw new InvalidOperationException(
                "Chỉ có thể tạm ngưng khi cửa hàng đang hoạt động.");

        Status = StoreStatusEnum.TemporarilyClosed;
    }

    public void ResumeOperations()
    {
        if (Status != StoreStatusEnum.TemporarilyClosed)
            throw new InvalidOperationException(
                "Cửa hàng phải ở trạng thái tạm ngưng mới có thể mở lại.");

        Status = StoreStatusEnum.Active;
    }

    public void CloseDown()
    {
        Status = StoreStatusEnum.ClosedDown;
    }

    public void SoftDelete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        Status = StoreStatusEnum.ClosedDown;
        DeletedAt = DateTime.UtcNow;
        foreach(var area in _areas)
        {
            area.Delete();
        }
    }

    // ---------------------------
    // DOMAIN VALIDATION
    // ---------------------------

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên cửa hàng không được để trống.");

        if (name.Length > 200)
            throw new ArgumentException("Tên cửa hàng quá dài.");
    }

    private static void ValidateStoreCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã cửa hàng không được để trống.");

        if (code.Length > 20)
            throw new ArgumentException("Mã cửa hàng quá dài.");
    }

    private static void ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Địa chỉ không được để trống.");
    }

    private static void ValidateGeo(double lat, double lng)
    {
        if (lat < 8.0 || lat > 24.0)
            throw new ArgumentOutOfRangeException(nameof(lat), "Vĩ độ không nằm trong lãnh thổ Việt Nam (8.0 đến 24.0).");

        if (lng < 102.0 || lng > 110.0)
            throw new ArgumentOutOfRangeException(nameof(lng), "Kinh độ không nằm trong lãnh thổ Việt Nam (102.0 đến 110.0).");
    }

    private void EnsureNotClosedPermanently()
    {
        if (Status == StoreStatusEnum.ClosedDown)
            throw new InvalidOperationException(
                "Cửa hàng đã bị đóng cửa vĩnh viễn.");
    }

    // ---------------------------
    // BEHAVIORS CHO ENTITY CON
    // ---------------------------

    // 1. Quản lý Giờ mở cửa
    public void SetOperatingHours(List<OperatingHourConfig> schedule)
    {
        _operatingHours.Clear(); // Xóa lịch cũ, cập nhật lịch mới
        foreach (var item in schedule)
        {
            if (_operatingHours.Any(h => h.DayOfWeek == item.Day))
                throw new InvalidOperationException($"Giờ mở cửa cho thứ {item.Day} đã được cấu hình.");

            if (item.IsClosed)
            {
                _operatingHours.Add(StoreOperatingHour.ClosedDay(this.Id, item.Day));
            }
            else
            {
                if (!item.Open.HasValue || !item.Close.HasValue)
                    throw new ArgumentException($"Phải cung cấp giờ mở/đóng cho ngày {item.Day} nếu cửa hàng có hoạt động.");

                _operatingHours.Add(new StoreOperatingHour(this.Id, item.Day, item.Open.Value, item.Close.Value));
            }
        }
    }

    // 2. Quản lý Khu vực (Area)
    public void AddArea(string name)
    {
        if (_areas.Any(a => a.Name.ToLower() == name.Trim().ToLowerInvariant() && !a.IsDeleted))
            throw new InvalidOperationException($"Khu vực '{name}' đã tồn tại trong cửa hàng này.");

        _areas.Add(new Area(this.Id, name));
    }

    public void UpdateArea(int areaId, string name, bool isActive)
    {
        var area = _areas.FirstOrDefault(a => a.Id == areaId);
        if (area == null)
            throw new ArgumentException("Không tìm thấy khu vực.");

        if (_areas.Any(a => a.Id != areaId && a.Name.Trim().ToLowerInvariant() == name.Trim().ToLowerInvariant() && !a.IsDeleted))
            throw new InvalidOperationException($"Khu vực '{name}' đã tồn tại trong cửa hàng này.");

        area.UpdateDetails(name, isActive);
    }

    public void RemoveArea(int areaId)
    {
        var area = _areas.FirstOrDefault(a => a.Id == areaId);
        if (area == null) throw new ArgumentException("Không tìm thấy khu vực.");

        area.Delete();
    }

    // ---------------------------
    // 3. Quản lý Bàn (Table)
    // ---------------------------

    public void AddTableToArea(int areaId, string tableName, int seatCapacity)
    {
        var area = _areas.FirstOrDefault(a => a.Id == areaId);
        if (area == null) throw new ArgumentException("Không tìm thấy khu vực.");

        area.AddTable(tableName, seatCapacity);
    }

    public void UpdateTable(int tableId, string name, int seatCapacity, bool isActive)
    {
        // Tìm xem cái bàn này đang nằm ở khu vực nào trong quán
        var area = _areas.FirstOrDefault(a => a.Tables.Any(t => t.Id == tableId && !t.IsDeleted));
        if (area == null) throw new ArgumentException("Không tìm thấy bàn trong cửa hàng này.");

        // Kiểm tra chống trùng tên bàn trong CÙNG MỘT KHU VỰC khi update
        if (area.Tables.Any(t => t.Id != tableId && t.Name.ToLower() == name.Trim().ToLower() && !t.IsDeleted))
            throw new InvalidOperationException($"Bàn '{name}' đã tồn tại trong khu vực này.");

        var table = area.Tables.First(t => t.Id == tableId);
        table.UpdateDetails(name, seatCapacity, isActive);
    }

    public void RemoveTable(int tableId)
    {
        var area = _areas.FirstOrDefault(a => a.Tables.Any(t => t.Id == tableId));
        if (area == null) throw new ArgumentException("Không tìm thấy bàn trong cửa hàng này.");

        area.RemoveTable(tableId);
    }

    public void RegenerateTableQrCode(int tableId)
    {
        var area = _areas.FirstOrDefault(a => a.Tables.Any(t => t.Id == tableId && !t.IsDeleted));
        if (area == null) throw new ArgumentException("Không tìm thấy bàn trong cửa hàng này.");

        var table = area.Tables.First(t => t.Id == tableId);
        table.RegenerateQrCode();
    }
}