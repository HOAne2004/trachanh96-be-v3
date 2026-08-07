using Shared.Domain.ValueObjects;
using Shared.Domain.Enums;

namespace Catalog.Domain.Entities;

public class ProductSize
{
    public Guid ProductId { get; private set; }
    public SizeEnum Size { get; private set; }

    // Đổi tên thành PriceModifier để tránh nhầm lẫn với Override giá gốc
    public Money PriceModifier { get; private set; }

    protected ProductSize()
    {
        PriceModifier = null!;
    }

    internal ProductSize(Guid productId, SizeEnum size, Money priceModifier)
    {
        ProductId = productId;
        Size = size;
        PriceModifier = priceModifier;
    }

    internal void UpdatePrice(Money newModifierPrice)
    {
        PriceModifier = newModifierPrice;
    }
}