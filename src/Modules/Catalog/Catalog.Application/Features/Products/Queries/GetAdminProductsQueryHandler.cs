using Catalog.Application.Interfaces;
using Catalog.Domain.Enums;
using MediatR;
using Shared.Application.Models;

namespace Catalog.Application.Features.Products.Queries;

public record AdminProductSummaryDto(
    Guid Id,
    string Name,
    string? ImageUrl,
    string ProductType,
    decimal BasePrice,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    int TotalSold // Giúp Admin biết món nào đang bán chạy/ế
);

public record GetAdminProductsQuery(
    string? SearchTerm = null,
    int? CategoryId = null,
    ProductTypeEnum? ProductType = null,
    ProductStatusEnum? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageIndex = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<AdminProductSummaryDto>>>;

public class GetAdminProductsQueryHandler : IRequestHandler<GetAdminProductsQuery, Result<PagedResult<AdminProductSummaryDto>>>
{
    private readonly IProductRepository _productRepository;

    public GetAdminProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<AdminProductSummaryDto>>> Handle(GetAdminProductsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize > 100 ? 100 : request.PageSize;
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;

        // Gọi DB (Dùng chung hàm Repository đã viết)
        var statuses = request.Status.HasValue ? new List<ProductStatusEnum> { request.Status.Value } : null;

        var (items, totalCount) = await _productRepository.GetPagedListAsync(
            request.SearchTerm, request.CategoryId, request.ProductType,
            statuses,
            request.FromDate, request.ToDate,
            pageIndex, pageSize, cancellationToken);

        // Map sang DTO riêng của Admin
        var dtos = items.Select(p => new AdminProductSummaryDto(
            Id: p.PublicId,
            Name: p.Name,
            ImageUrl: p.ImageUrl,
            ProductType: p.ProductType.ToString(),
            BasePrice: p.BasePrice.Amount,
            Currency: p.BasePrice.Currency,
            Status: p.Status.ToString(),
            CreatedAt: p.CreatedAt,
            PublishedAt: p.PublishedAt,
            TotalSold: p.TotalSold // Cột mới thêm
        )).ToList();

        return Result<PagedResult<AdminProductSummaryDto>>.Success(
            new PagedResult<AdminProductSummaryDto>(dtos, totalCount, pageIndex, pageSize));
    }
}