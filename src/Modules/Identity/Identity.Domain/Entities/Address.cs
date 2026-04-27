using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Identity.Domain.ValueObjects;
using Shared.Domain.SeedWork;
using Shared.Domain.Interfaces;

namespace Identity.Domain.Entities; 

public class Address : Entity<int>, IAuditableEntity
{
    public string RecipientName { get; private set; }
    public PhoneNumber RecipientPhone { get; private set; }

    public string AddressDetail { get; private set; }
    public string Province { get; private set; }
    public string District { get; private set; }
    public string Commune { get; private set; }

    public string FullAddress => string.Join(", ", new[] { AddressDetail, Commune, District, Province }
                                    .Where(s => !string.IsNullOrWhiteSpace(s)));
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public bool IsDefault { get; private set; }

    // --- Tự động quản lý bởi Interceptor ---
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    protected Address()
    {
        RecipientName = null!;
        RecipientPhone = null!;
        AddressDetail = null!;
        Province = null!;
        District = null!;
        Commune = null!;
    }
    // Constructor internal: Ép buộc khởi tạo qua User
    internal Address(string recipientName, string rawPhone, string addressDetail,
                     string province, string district, string commune,
                     double? latitude, double? longitude, bool isDefault)
    {
        Update(recipientName, rawPhone, addressDetail, province, district, commune, latitude, longitude);
        IsDefault = isDefault;
    }

    [MemberNotNull(nameof(RecipientName), nameof(RecipientPhone),
                   nameof(AddressDetail), nameof(Province),
                   nameof(District), nameof(Commune))]
    internal void Update(string recipientName, string rawPhone, string addressDetail,
                         string province, string district, string commune,
                         double? latitude, double? longitude)
    {
        if (string.IsNullOrWhiteSpace(recipientName) || recipientName.Length > 150)
            throw new ArgumentException("Tên người nhận không hợp lệ hoặc quá dài (tối đa 150 ký tự).");

        if (string.IsNullOrWhiteSpace(addressDetail) || addressDetail.Length > 300)
            throw new ArgumentException("Địa chỉ chi tiết không hợp lệ hoặc quá dài (tối đa 300 ký tự).");

        if (string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(district) || string.IsNullOrWhiteSpace(commune))
            throw new ArgumentException("Khu vực hành chính không được để trống.");

        if (latitude.HasValue && (latitude < -90 || latitude > 90))
            throw new ArgumentException("Vĩ độ (Latitude) không hợp lệ.");

        if (longitude.HasValue && (longitude < -180 || longitude > 180))
            throw new ArgumentException("Kinh độ (Longitude) không hợp lệ.");

        RecipientName = recipientName.Trim();
        RecipientPhone = PhoneNumber.Create(rawPhone); // Tự động parse và validate
        AddressDetail = addressDetail.Trim();

        // Chuẩn hóa string cho data sạch
        Province = province.Trim();
        District = district.Trim();
        Commune = commune.Trim();

        Latitude = latitude;
        Longitude = longitude;
    }
    internal void SetAsDefault() => IsDefault = true;
    internal void RemoveDefault() => IsDefault = false;
}