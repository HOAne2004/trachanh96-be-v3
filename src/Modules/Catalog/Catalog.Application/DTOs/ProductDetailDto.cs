
using Catalog.Domain.Enums;
using MediatR;
using Shared.Application.Models;
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
        decimal PriceAmount,
        int MaxQuantity,
        string Currency
    );

    public record ProductDetailDto(
        Guid Id,
        int CategoryId,
        string Name,
        Slug Slug,
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
        List<ProductToppingDto> Toppings
    );

}
