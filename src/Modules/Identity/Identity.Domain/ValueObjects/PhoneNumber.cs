using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

public record PhoneNumber
{
    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            throw new ArgumentException("Số điện thoại không được để trống.");

        // Bỏ tất cả ký tự không phải số
        var digits = Regex.Replace(rawPhone, @"\D", "");

        if (digits.StartsWith('0'))
        {
            digits = "84" + digits[1..];
        }
        else if (digits.StartsWith("84"))
        {
            // Giữ nguyên
        }
        else
        {
            throw new ArgumentException("Số điện thoại phải bắt đầu bằng 0 hoặc 84 (đối với VN).");
        }

        var normalized = "+" + digits;

        // Valid length theo chuẩn quốc tế + VN
        if (normalized.Length < 11 || normalized.Length > 15)
            throw new ArgumentException("Độ dài số điện thoại không hợp lệ.");

        return new PhoneNumber(normalized);
    }

    // Tiện ích cho UI hiển thị (0981 234 567)
    public string ToFormattedString()
    {
        if (Value.StartsWith("+84") && Value.Length == 12)
        {
            var local = "0" + Value[3..];
            return $"{local[..4]} {local[4..7]} {local[7..]}";
        }
        return Value;
    }

    // override ToString để dễ gọi
    public override string ToString() => Value;
}