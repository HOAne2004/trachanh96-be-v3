using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;

namespace Orders.Domain.ValueObjects;

public class DeliveryInfo : ValueObject
{
    public string RecipientName { get; private set; }
    public string PhoneNumber { get; private set; }
    public string Address { get; private set; }
    public DateTime? PickupTime { get; private set; }

    // Các trường mới phục vụ mở rộng (Tracking)
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double? DistanceKm { get; private set; }
    public string? ProviderName { get; private set; }
    public string? TrackingId { get; private set; }

    private DeliveryInfo()
    {
        RecipientName = null!; PhoneNumber = null!; Address = null!;
    }

    public static DeliveryInfo Create(
        string name,
        string phone,
        string address,
        DateTime? pickupTime = null,
        double? latitude = null,
        double? longitude = null,
        double? distanceKm = null,
        string? providerName = null,
        string? trackingId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên người nhận không được để trống.");

        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 9)
            throw new DomainException("Số điện thoại không hợp lệ.");

        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Địa chỉ giao hàng không được để trống.");

        return new DeliveryInfo
        {
            RecipientName = name.Trim(),
            PhoneNumber = phone.Trim(),
            Address = address.Trim(),
            PickupTime = pickupTime,
            Latitude = latitude,
            Longitude = longitude,
            DistanceKm = distanceKm,
            ProviderName = providerName?.Trim(),
            TrackingId = trackingId?.Trim()
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RecipientName;
        yield return PhoneNumber;
        yield return Address;
        if (PickupTime.HasValue) yield return PickupTime.Value;
        if (Latitude.HasValue) yield return Latitude.Value;
        if (Longitude.HasValue) yield return Longitude.Value;
        if(DistanceKm.HasValue) yield return DistanceKm.Value;
        if (ProviderName != null) yield return ProviderName;
        if (TrackingId != null) yield return TrackingId;
    }
}