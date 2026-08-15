using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Identity.Domain.ValueObjects;
using Shared.Domain.SeedWork;
using Shared.Domain.Interfaces;
using Shared.Domain.Exceptions;

namespace Identity.Domain.Entities;

public class Address : AuditableEntity<Guid>
{
    public string RecipientName { get; private set; }
    public PhoneNumber RecipientPhone { get; private set; }
    public string AddressDetail { get; private set; }
    public string Province { get; private set; }
    public string? District { get; private set; }
    public string Commune { get; private set; }

    public string FullAddress => string.Join(", ", new[] { AddressDetail, Commune, District, Province }
                                    .Where(s => !string.IsNullOrWhiteSpace(s)));
    public GeoLocation? Location { get; private set; }

    public bool IsDefault { get; private set; }

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
                     string province, string? district, string commune,
                     double? latitude, double? longitude, bool isDefault)
    {
        Id = Guid.CreateVersion7();
        Update(recipientName, rawPhone, addressDetail, province, district, commune, latitude, longitude);
        IsDefault = isDefault;
    }

    [MemberNotNull(nameof(RecipientName), nameof(RecipientPhone),
                   nameof(AddressDetail), nameof(Province),
                   nameof(District), nameof(Commune))]
    internal void Update(string recipientName, string rawPhone, string addressDetail,
                         string province, string? district, string commune,
                         double? latitude, double? longitude)
    {
        if (string.IsNullOrWhiteSpace(recipientName) || recipientName.Length > 150)
            throw new DomainException("Tên người nhận không hợp lệ hoặc quá dài (tối đa 150 ký tự).");

        if (string.IsNullOrWhiteSpace(addressDetail) || addressDetail.Length > 300)
            throw new DomainException("Địa chỉ chi tiết không hợp lệ hoặc quá dài (tối đa 300 ký tự).");

        if (string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(district) || string.IsNullOrWhiteSpace(commune))
            throw new DomainException("Khu vực hành chính không được để trống.");

        RecipientName = recipientName.Trim();
        RecipientPhone = PhoneNumber.Create(rawPhone); // Tự động parse và validate
        AddressDetail = addressDetail.Trim();

        // Chuẩn hóa string cho data sạch
        Province = province.Trim();
        District = district.Trim();
        Commune = commune.Trim();

        Location = GeoLocation.Create(latitude, longitude);
    }
    internal void SetAsDefault() => IsDefault = true;
    internal void RemoveDefault() => IsDefault = false;
}