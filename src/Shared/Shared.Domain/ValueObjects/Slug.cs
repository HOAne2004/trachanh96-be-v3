using System.Text.RegularExpressions;
using System.Text;
using Shared.Domain.SeedWork;

namespace Shared.Domain.ValueObjects;

public sealed class Slug : ValueObject
{
    // Tối ưu Production: Dùng Compiled để tránh cấp phát bộ nhớ lúc runtime
    private static readonly Regex InvalidCharsRegex = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
    private static readonly Regex SpacesRegex = new(@"[\s-]+", RegexOptions.Compiled);

    public const int MaxLength = 200;

    public string Value { get; }

    private Slug(string value)
    {
        Value = value;
    }

    // Hỗ trợ tự động tạo (Auto-generate)
    public static Slug Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Đầu vào tạo Slug không được để trống.");

        var generatedSlug = GenerateSlug(input);
        return new Slug(generatedSlug);
    }

    // Hỗ trợ ghi đè thủ công (Dành cho Admin/SEO)
    public static Slug CreateManual(string manualSlug)
    {
        if (string.IsNullOrWhiteSpace(manualSlug))
            throw new ArgumentException("Slug thủ công không được để trống.");

        // Vẫn phải đảm bảo Slug thủ công hợp lệ
        var safeSlug = GenerateSlug(manualSlug);
        return new Slug(safeSlug);
    }

    private static string GenerateSlug(string phrase)
    {
        string str = RemoveDiacritics(phrase).ToLowerInvariant();
        str = InvalidCharsRegex.Replace(str, "");
        str = SpacesRegex.Replace(str, " ").Trim();
        str = str.Replace(" ", "-");

        // Tối ưu Production: Cắt bớt nếu quá dài để an toàn cho Database
        if (str.Length > MaxLength)
            str = str.Substring(0, MaxLength).TrimEnd('-');

        return str;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC)
            .Replace("đ", "d").Replace("Đ", "d");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}