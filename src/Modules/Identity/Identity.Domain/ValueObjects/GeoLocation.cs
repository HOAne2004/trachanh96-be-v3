using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;

namespace Identity.Domain.ValueObjects;

public sealed class GeoLocation : ValueObject
{
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private GeoLocation() { }

    private GeoLocation(double? latitude, double? longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static GeoLocation? Create(double? latitude, double? longitude)
    {
        // Cả hai đều rỗng: không có tọa độ, hợp lệ -> trả về null (Address không bắt buộc phải có GPS)
        if (!latitude.HasValue && !longitude.HasValue) return null;

        // Chỉ có 1 trong 2 giá trị: đây là dữ liệu đầu vào không nhất quán, cần báo lỗi thay vì
        // âm thầm trả về null (bản gốc coi thiếu 1 giá trị = coi như không có gì, làm mất giá trị
        // người dùng đã nhập mà không có bất kỳ cảnh báo nào).
        if (!latitude.HasValue || !longitude.HasValue)
            throw new DomainException("Cần nhập đầy đủ cả Vĩ độ và Kinh độ, hoặc để trống cả hai.");

        if (latitude < -90 || latitude > 90)
            throw new DomainException("Vĩ độ (Latitude) không hợp lệ.");

        if (longitude < -180 || longitude > 180)
            throw new DomainException("Kinh độ (Longitude) không hợp lệ.");

        return new GeoLocation(latitude.Value, longitude.Value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}