namespace Catalog.Domain.Entities;

public class StoreProduct
{
    public Guid StoreId { get; private set; } // ID từ module Stores
    public int ProductId { get; private set; }
    public Product Product { get; private set; }

    // Logic nghiệp vụ cấu hình
    public decimal? PriceOverride { get; private set; } // Giá gốc riêng tại quán (nếu có)
    public bool IsAvailable { get; private set; } // Tình trạng còn/hết tại quán này
    public bool IsActive { get; private set; } // Quán có kinh doanh món này không

    // Thống kê cục bộ tại quán
    public int SoldCount { get; private set; } = 0;
    public double TotalRatingScore { get; private set; } = 0; // Tổng số sao tích lũy
    public int RatingCount { get; private set; } = 0;         // Tổng số lượt đánh giá

    // Tính điểm trung bình an toàn (tránh chia cho 0)
    public double AverageRating => RatingCount == 0 ? 0 : Math.Round(TotalRatingScore / RatingCount, 1);

    protected StoreProduct()
    {
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

        SoldCount = 0;
        TotalRatingScore = 0;
        RatingCount = 0;
    }

    public void UpdateStatus(bool available, bool active)
    {
        IsAvailable = available;
        IsActive = active;
    }

    // Các hàm cập nhật số liệu (Domain Behaviors)
    public void IncrementSold(int quantity)
    {
        if (quantity > 0) SoldCount += quantity;
    }

    public void AddRating(double stars)
    {
        if (stars >= 0 && stars <= 5)
        {
            TotalRatingScore += stars;
            RatingCount++;
        }
    }
}