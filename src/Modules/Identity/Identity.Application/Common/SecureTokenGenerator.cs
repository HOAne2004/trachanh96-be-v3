using System.Security.Cryptography;

namespace Identity.Application.Common;

/// <summary>
/// Sinh chuỗi ngẫu nhiên dùng cho mã xác thực (reset password OTP, verify email token)
/// bằng CSPRNG (RandomNumberGenerator) - nhất quán với cách IJwtProvider.GenerateRefreshToken()
/// đã làm, thay vì Guid.NewGuid() (không có cam kết là CSPRNG trên mọi nền tảng).
/// </summary>
public static class SecureTokenGenerator
{
    // Bỏ ký tự dễ nhầm khi đọc bằng mắt: 0/O, 1/I/L
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private const string DigitAlphabet = "0123456789";

    /// <summary>Mã ngắn, dễ đọc/gõ tay - dùng cho OTP reset password gửi qua email.</summary>
    public static string GenerateReadableCode(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }
        return new string(chars);
    }

    /// <summary>
    /// Mã OTP thuần chữ số (0-9), dùng cho các email/SMS đã cam kết định dạng "N chữ số"
    /// với người dùng (VD: ResetPassword.cshtml ghi rõ "mã OTP gồm 6 chữ số").
    /// </summary>
    public static string GenerateNumericCode(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = DigitAlphabet[bytes[i] % 10];
        }
        return new string(chars);
    }

    /// <summary>Token dài, không cần dễ đọc - dùng cho link xác thực email.</summary>
    public static string GenerateUrlSafeToken(int byteLength = 32)
    {
        var bytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}