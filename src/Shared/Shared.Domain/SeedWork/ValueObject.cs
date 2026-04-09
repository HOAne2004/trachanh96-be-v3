/// <summary>
/// [DDD SEEDWORK: VALUE OBJECT]
/// Chức năng: Lớp nền tảng cho các Đối tượng Giá trị.
/// Đặc điểm:
/// - Không có danh tính (No Identity - không có Id).
/// - Tính bất biến (Immutable): Khi đã tạo ra thì không được sửa (set). Muốn đổi giá trị thì phải tạo một Object mới.
/// - So sánh theo giá trị (Structural Equality): Hai Value Object bằng nhau khi TẤT CẢ các thuộc tính (components) của chúng bằng nhau.
/// Sử dụng: Triển khai hàm GetEqualityComponents để liệt kê các thuộc tính cần đem ra so sánh.
/// </summary>

namespace Shared.Domain.SeedWork;

public abstract class ValueObject : IEquatable<ValueObject>
{
    // Bắt buộc các class con phải khai báo những thuộc tính nào dùng để so sánh
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var valueObject = (ValueObject)obj;

        // So sánh từng thành phần (component) với nhau
        return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }

    public bool Equals(ValueObject? other)
    {
        return Equals((object?)other);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            return true;

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}