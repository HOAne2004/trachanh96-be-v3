using Catalog.Application.Interfaces;
using Catalog.Domain.Enums;
using MediatR;
using Shared.Application.Models;

namespace Catalog.Application.Features.Products.Queries;

public record CustomerProductCardDto(
    Guid Id,
    int CategoryId,
    string Name,
    string Slug, 
    string? ImageUrl,
    decimal BasePrice,
    string Currency,
    int TotalSold,
    double AverageRating,
    int RatingCount,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    string status
);

public record GetCatalogProductsQuery(
    string? SearchTerm = null,
    int? CategoryId = null,
    Guid? StoreId = null,
    int PageIndex = 1,
    int PageSize = 12 
) : IRequest<Result<PagedResult<CustomerProductCardDto>>>;

public class GetCatalogProductsQueryHandler : IRequestHandler<GetCatalogProductsQuery, Result<PagedResult<CustomerProductCardDto>>>
{
    private readonly IProductRepository _productRepository;

    public GetCatalogProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<CustomerProductCardDto>>> Handle(GetCatalogProductsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize > 50 ? 50 : request.PageSize;
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;

        var allowedStatuses = new List<ProductStatusEnum>
        {
            ProductStatusEnum.Active,
            ProductStatusEnum.Inactive,
            ProductStatusEnum.ComingSoon
        };

        var (items, totalCount) = await _productRepository.GetPagedListAsync(
            request.SearchTerm, request.CategoryId, request.StoreId, null,
            allowedStatuses, // Cho phép 3 trạng thái
            null, null,
            pageIndex, pageSize, cancellationToken);

        // Map sang DTO riêng của Customer
        var dtos = items.Select(p =>
        {
            // 1. Lấy cấu hình riêng của sản phẩm tại Cửa hàng đang được chọn
            var storeInfo = request.StoreId.HasValue
                ? p.StoreProducts.FirstOrDefault(sp => sp.StoreId == request.StoreId.Value)
                : null;

            return new CustomerProductCardDto(
                Id: p.PublicId,
                CategoryId: p.CategoryId,
                Name: p.Name,
                Slug: p.Slug.Value,
                ImageUrl: p.ImageUrl,

                BasePrice: storeInfo?.PriceOverride ?? p.BasePrice.Amount,
                Currency: p.BasePrice.Currency,

                TotalSold: storeInfo != null ? storeInfo.SoldCount : p.TotalSold,
                AverageRating: storeInfo != null ? storeInfo.AverageRating : p.AverageRating,
                RatingCount: storeInfo != null ? storeInfo.RatingCount : p.RatingCount,

                CreatedAt: p.CreatedAt,
                PublishedAt: p.PublishedAt,
                status: (storeInfo != null && !storeInfo.IsAvailable) ? "OutOfStock" : p.Status.ToString()
            );
                }).ToList();

        return Result<PagedResult<CustomerProductCardDto>>.Success(
            new PagedResult<CustomerProductCardDto>(dtos, totalCount, pageIndex, pageSize));
    }
}  