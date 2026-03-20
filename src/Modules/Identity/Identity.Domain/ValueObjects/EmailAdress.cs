using System.Text.RegularExpressions;
using Shared.Domain;

namespace Identity.Domain.ValueObjects;

public sealed class EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email không được để trống.");

        var formattedEmail = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(formattedEmail))
            throw new ArgumentException("Định dạng email không hợp lệ.");

        return new EmailAddress(formattedEmail);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}