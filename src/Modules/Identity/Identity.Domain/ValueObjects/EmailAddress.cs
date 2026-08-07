using System.Text.RegularExpressions;
using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;

namespace Identity.Domain.ValueObjects;

public sealed class EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    private const int MaxLength = 255; // Khớp UserConfiguration.HasMaxLength(255)

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email không được để trống.");

        var formattedEmail = email.Trim().ToLowerInvariant();

        if (formattedEmail.Length > MaxLength)
            throw new DomainException($"Email không được vượt quá {MaxLength} ký tự.");

        if (!EmailRegex.IsMatch(formattedEmail))
            throw new DomainException("Định dạng email không hợp lệ.");

        return new EmailAddress(formattedEmail);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}