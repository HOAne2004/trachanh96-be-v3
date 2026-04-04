namespace Shared.Domain.Utilities;

public static class GeoDistanceCalculator
{
    // Bán kính trung bình của Trái Đất tính bằng Kilometer
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Tính khoảng cách đường chim bay giữa 2 tọa độ GPS (Công thức Haversine)
    /// </summary>
    /// <returns>Khoảng cách tính bằng Kilometer (km)</returns>
    public static double CalculateDistanceInKm(double lat1, double lon1, double lat2, double lon2)
    {
        // 1. Chuyển đổi chênh lệch độ sang Radian
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        // 2. Chuyển đổi vĩ độ sang Radian
        var radLat1 = DegreesToRadians(lat1);
        var radLat2 = DegreesToRadians(lat2);

        // 3. Áp dụng công thức Haversine
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
                Math.Cos(radLat1) * Math.Cos(radLat2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        // 4. Trả về khoảng cách
        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Hàm hỗ trợ chuyển đổi từ Độ (Degrees) sang Radian
    /// </summary>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}