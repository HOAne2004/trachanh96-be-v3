namespace Catalog.Domain.Entities;

public class StoreProduct
{
    public Guid StoreId { get; private set; } // ID từ module Stores
    public int ProductId { get; private set; }
    public Product Product { get; private set; }

    // Logic nghiệp vụ quan trọng
    public decimal? PriceOverride { get; private set; } // Giá riêng tại quán (nếu có)
    public bool IsAvailable { get; private set; } // Tình trạng còn/hết tại quán này
    public bool IsActive { get; private set; } // Quán có kinh doanh món này không

    protected StoreProduct() {
        Product = null!;
    }

    public StoreProduct(Guid storeId, int productId, Product product, decimal? priceOverride = null)
    {
        StoreId = storeId;
        ProductId = productId;
        Product = product;
        PriceOverride = priceOverride;
        IsAvailable = true;
        IsActive = true;
    }

    public void UpdateStatus(bool available, bool active)
    {
        IsAvailable = available;
        IsActive = active;
    }
}