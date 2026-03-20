using Shared.Domain;

namespace Stores.Domain.Entities;

public class StoreOperatingHour : Entity<int>
{
    public int StoreId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }

    public TimeSpan? OpenTime { get; private set; }
    public TimeSpan? CloseTime { get; private set; }

    public bool IsClosed { get; private set; }

    protected StoreOperatingHour() { }

    // Constructor cho ngày mở cửa bình thường
    internal StoreOperatingHour(
        int storeId,
        DayOfWeek dayOfWeek,
        TimeSpan openTime,
        TimeSpan closeTime)
    {
        ValidateTime(openTime, closeTime);

        StoreId = storeId;
        DayOfWeek = dayOfWeek;

        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = false;
    }

    // Constructor riêng (ẩn) cho ngày nghỉ
    private StoreOperatingHour(int storeId, DayOfWeek dayOfWeek)
    {
        StoreId = storeId;
        DayOfWeek = dayOfWeek;
        IsClosed = true;
        OpenTime = null;
        CloseTime = null;
    }

    internal static StoreOperatingHour ClosedDay(int storeId, DayOfWeek day)
    {
        return new StoreOperatingHour(storeId, day);
    }

    internal void UpdateHours(TimeSpan openTime, TimeSpan closeTime)
    {
        ValidateTime(openTime, closeTime);

        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = false;
    }

    internal void MarkClosed()
    {
        IsClosed = true;
        OpenTime = null;
        CloseTime = null;
    }

    public bool IsOpenAt(TimeSpan time)
    {
        if (IsClosed || !OpenTime.HasValue || !CloseTime.HasValue)
            return false;

        if (OpenTime.Value <= CloseTime.Value)
            return time >= OpenTime.Value && time <= CloseTime.Value;

        // Xử lý ca làm việc xuyên đêm (VD: Mở 18:00, Đóng 02:00 sáng hôm sau)
        return time >= OpenTime.Value || time <= CloseTime.Value;
    }

    private static void ValidateTime(TimeSpan open, TimeSpan close)
    {
        if (open < TimeSpan.Zero || open >= TimeSpan.FromDays(1))
            throw new ArgumentException("Thời gian mở cửa phải nằm trong vòng một ngày.");

        if (close < TimeSpan.Zero || close >= TimeSpan.FromDays(1))
            throw new ArgumentException("Thời gian đóng cửa phải nằm trong vòng một ngày.");

        if (open == close)
            throw new ArgumentException("Thời gian mở và thời gian đóng không thể bằng nhau.");
    }
}