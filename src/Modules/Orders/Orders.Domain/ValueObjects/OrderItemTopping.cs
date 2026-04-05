using Shared.Domain.SeedWork;
using Shared.Domain.ValueObjects;

namespace Orders.Domain.ValueObjects;

public class OrderItemTopping : ValueObject
{
    public Guid ToppingId { get; }
    public string ToppingName { get; }
    public Money Price { get; }
    public int Quantity { get; }

    private OrderItemTopping() { ToppingName = null!; Price = null!; }

    public OrderItemTopping(Guid toppingId, string toppingName, Money price, int quantity)
    {
        if (price.Amount < 0)
            throw new ArgumentException("Price must be >= 0");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be > 0");

        ToppingId = toppingId;
        ToppingName = toppingName;
        Price = price;
        Quantity = quantity;
    }

    // Merge Topping trùng lặp
    public OrderItemTopping AddQuantity(int additionalQuantity)
    {
        return new OrderItemTopping(ToppingId, ToppingName, Price, Quantity + additionalQuantity);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ToppingId;
        yield return ToppingName; 
        yield return Price;
        yield return Quantity;
    }
}