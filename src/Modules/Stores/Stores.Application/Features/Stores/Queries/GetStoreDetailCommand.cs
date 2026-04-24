using MediatR;
using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Stores.Queries;

public record GetStoreDetailQuery(Guid PublicId) : IRequest<Result<StoreAdminDetailDto>>;

public class GetStoreDetailQueryHandler : IRequestHandler<GetStoreDetailQuery, Result<StoreAdminDetailDto>>
{
    private readonly IStoreQueryService _queryService;

    public GetStoreDetailQueryHandler(IStoreQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<StoreAdminDetailDto>> Handle(GetStoreDetailQuery request, CancellationToken cancellationToken)
    {
        var store = await _queryService.GetStoreDetailAsync(request.PublicId, cancellationToken);

        if (store == null)
        {
            return Result<StoreAdminDetailDto>.Failure("Không tìm thấy cửa hàng hoặc cửa hàng đã bị xóa.");
        }

        return Result<StoreAdminDetailDto>.Success(store);
    }
}