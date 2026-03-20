using Shared.Domain.ValueObjects;

namespace Catalog.Domain.Entities;

public class ProductTopping
{
    public int ProductId { get; private set; }
    public int ToppingId { get; private set; }

    public Money PriceOverride { get; private set; }

    public int MaxQuantity { get; private set; }

    protected ProductTopping()
    {
        PriceOverride = null!;
    }

    internal ProductTopping(int productId, int toppingId, Money priceOverride, int maxQuantity)
    {
        if (maxQuantity < 1)
            throw new ArgumentException("Số lượng Topping tối đa phải lớn hơn 0.");

        ProductId = productId;
        ToppingId = toppingId;
        PriceOverride = priceOverride;
        MaxQuantity = maxQuantity;
    }

    internal void Update(Money newPrice, int newMaxQuantity)
    {
        if (newMaxQuantity < 1)
            throw new ArgumentException("Số lượng Topping tối đa phải lớn hơn 0.");

        PriceOverride = newPrice;
        MaxQuantity = newMaxQuantity;
    }
}