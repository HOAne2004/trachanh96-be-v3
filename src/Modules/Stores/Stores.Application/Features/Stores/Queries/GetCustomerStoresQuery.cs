using MediatR;
using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Stores.Queries;

public record GetCustomerStoresQuery(
    double? UserLatitude,
    double? UserLongitude,
    string? SearchTerm,
    int PageIndex = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<StoreCustomerListDto>>>;

public class GetCustomerStoresQueryHandler : IRequestHandler<GetCustomerStoresQuery, Result<PagedResult<StoreCustomerListDto>>>
{
    private readonly IStoreQueryService _queryService;

    public GetCustomerStoresQueryHandler(IStoreQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<PagedResult<StoreCustomerListDto>>> Handle(GetCustomerStoresQuery request, CancellationToken cancellationToken)
    {
        // Giao toàn bộ việc query DB, tính toán khoảng cách (Haversine/PostGIS) 
        // và đối chiếu giờ giấc cho tầng Infrastructure.
        var pagedResult = await _queryService.GetPagedCustomerStoresAsync(
            request.UserLatitude,
            request.UserLongitude,
            request.SearchTerm,
            request.PageIndex,
            request.PageSize,
            cancellationToken
        );

        return Result<PagedResult<StoreCustomerListDto>>.Success(pagedResult);
    }
}