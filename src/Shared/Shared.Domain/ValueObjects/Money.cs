using Shared.Domain.SeedWork;

namespace Shared.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "VND")
    {
        if (amount < 0)
            throw new ArgumentException("Giá tiền không được nhỏ hơn 0.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Đơn vị tiền tệ không hợp lệ.");

        // Tối ưu Production: Làm tròn 0 chữ số thập phân cho VND, 2 cho các loại khác
        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        var roundedAmount = normalizedCurrency == "VND" ? Math.Round(amount, 0) : Math.Round(amount, 2);

        return new Money(roundedAmount, normalizedCurrency);
    }

    // Tiện ích kinh điển cho Production
    public static Money Zero(string currency = "VND") => new Money(0, currency.ToUpperInvariant());

    // Nhân với số lượng
    public Money Multiply(int quantity)
    {
        if (quantity < 0) throw new ArgumentException("Số lượng không thể âm.");
        return Create(Amount * quantity, Currency);
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return Create(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        if (left.Amount < right.Amount)
            throw new InvalidOperationException("Kết quả phép trừ không thể tạo ra số tiền âm.");

        return Create(left.Amount - right.Amount, left.Currency);
    }

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Lệch tiền tệ: Không thể tính toán {a.Currency} và {b.Currency}.");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}