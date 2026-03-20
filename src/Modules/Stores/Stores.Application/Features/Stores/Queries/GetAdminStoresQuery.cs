using MediatR;
using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Application.Interfaces;
using Stores.Domain.Enums;

namespace Stores.Application.Features.Stores.Queries
{
    public record GetAdminStoresQuery(
    string? SearchTerm,
    StoreStatusEnum? Status,
    int PageIndex = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<StoreAdminListDto>>>;

    public class GetAdminStoresQueryHandler : IRequestHandler<GetAdminStoresQuery, Result<PagedResult<StoreAdminListDto>>>
    {
        private readonly IStoreQueryService _queryService;

        public GetAdminStoresQueryHandler(IStoreQueryService queryService)
        {
            _queryService = queryService;
        }

        public async Task<Result<PagedResult<StoreAdminListDto>>> Handle(GetAdminStoresQuery request, CancellationToken cancellationToken)
        {
            // Giao toàn bộ việc truy vấn và tối ưu SQL cho tầng Infrastructure (StoreQueryService)
            var pagedResult = await _queryService.GetPagedAdminStoresAsync(
                request.SearchTerm,
                request.Status,
                request.PageIndex,
                request.PageSize,
                cancellationToken
            );

            return Result<PagedResult<StoreAdminListDto>>.Success(pagedResult);
        }
    }
}