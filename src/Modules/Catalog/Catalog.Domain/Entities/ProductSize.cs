using Shared.Domain.ValueObjects;
using Catalog.Domain.Enums;

namespace Catalog.Domain.Entities;

public class ProductSize
{
    public int ProductId { get; private set; }
    public SizeEnum Size{ get; private set; }

    public Money PriceOverride { get; private set; }

    protected ProductSize()
    {
        PriceOverride = null!;
    }

    internal ProductSize(int productId, SizeEnum size, Money priceOverride)
    {
        ProductId = productId;
        Size = size;
        PriceOverride = priceOverride;
    }

    internal void UpdatePrice(Money newPrice)
    {
        PriceOverride = newPrice;
    }
}