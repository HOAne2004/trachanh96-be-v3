
using Shared.Domain.ValueObjects;

namespace Catalog.Application.DTOs
{
    public record ProductSizeDto(
    string Size,
    decimal PriceAmount,
    string Currency
);

    public record ProductToppingDto(
        int ToppingId,
        string Name,
        string? ImageUrl,
        decimal PriceAmount,
        int MaxQuantity,
        string Currency

    );

    public record ProductDetailDto(
        Guid Id,
        int CategoryId,
        Guid? StoreId,
        string Name,
        string Slug,
        string? Description,
        string? Ingredients,
        string? ImageUrl,
        string ProductType,
        decimal BasePriceAmount,
        string BasePriceCurrency,
        int PrepTimeInMinutes,
        string Status,
        List<string> AllowedIceLevels,  
        List<string> AllowedSugarLevels,
        List<ProductSizeDto> Sizes,
        List<ProductToppingDto> Toppings,
        int TotalSold,
        double TotalRating,
        DateTime? PublishedAt,
        DateTime? CreatedAt
    );

}
