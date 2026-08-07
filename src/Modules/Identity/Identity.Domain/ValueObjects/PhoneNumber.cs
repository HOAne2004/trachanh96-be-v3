using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;
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
            throw new DomainException("Số điện thoại không được để trống.");

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
            throw new DomainException("Số điện thoại phải bắt đầu bằng 0 hoặc 84 (đối với VN).");
        }

        var normalized = "+" + digits;

        if (normalized.Length < 11 || normalized.Length > 15)
            throw new DomainException("Độ dài số điện thoại không hợp lệ.");

        return new PhoneNumber(normalized);
    }

    public string ToFormattedString()
    {
        if (Value.StartsWith("+84") && Value.Length == 12)
        {
            var local = "0" + Value[3..];
            return $"{local[..4]} {local[4..7]} {local[7..]}";
        }
        return Value;
    }

    public override string ToString() => Value;
}