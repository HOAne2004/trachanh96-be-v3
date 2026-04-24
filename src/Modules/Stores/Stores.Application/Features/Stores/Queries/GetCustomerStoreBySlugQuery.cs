using MediatR;
using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Stores.Queries;

public record GetCustomerStoreBySlugQuery(string Slug) : IRequest<Result<StoreCustomerDetailDto>>;

public class GetCustomerStoreBySlugQueryHandler : IRequestHandler<GetCustomerStoreBySlugQuery, Result<StoreCustomerDetailDto>>
{
    private readonly IStoreQueryService _queryService;

    public GetCustomerStoreBySlugQueryHandler(IStoreQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<StoreCustomerDetailDto>> Handle(GetCustomerStoreBySlugQuery request, CancellationToken cancellationToken)
    {
        var store = await _queryService.GetCustomerStoreBySlugAsync(request.Slug, cancellationToken);

        if (store == null)
        {
            return Result<StoreCustomerDetailDto>.Failure("Không tìm thấy cửa hàng hoặc cửa hàng đang tạm nghỉ.");
        }

        return Result<StoreCustomerDetailDto>.Success(store);
    }
}