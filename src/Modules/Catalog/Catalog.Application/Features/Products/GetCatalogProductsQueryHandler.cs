using Catalog.Application.Interfaces;
using Catalog.Domain.Enums;
using MediatR;
using Shared.Application.Models;

namespace Catalog.Application.Features.Products.Queries.GetCatalogProducts;

public record CustomerProductCardDto(
    Guid Id,
    string Name,
    string Slug, 
    string? ImageUrl,
    decimal BasePrice,
    string Currency,
    int TotalSold,
    double TotalRating
);

public record GetCatalogProductsQuery(
    string? SearchTerm = null,
    int? CategoryId = null,
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
            request.SearchTerm, request.CategoryId, null,
            allowedStatuses, // Cho phép 3 trạng thái
            null, null,
            pageIndex, pageSize, cancellationToken);

        // Map sang DTO riêng của Customer
        var dtos = items.Select(p => new CustomerProductCardDto(
            Id: p.PublicId,
            Name: p.Name,
            Slug: p.Slug.Value,
            ImageUrl: p.ImageUrl,
            BasePrice: p.BasePrice.Amount,
            Currency: p.BasePrice.Currency,
            TotalSold: p.TotalSold,
            TotalRating: p.TotalRating
        )).ToList();

        return Result<PagedResult<CustomerProductCardDto>>.Success(
            new PagedResult<CustomerProductCardDto>(dtos, totalCount, pageIndex, pageSize));
    }
}  