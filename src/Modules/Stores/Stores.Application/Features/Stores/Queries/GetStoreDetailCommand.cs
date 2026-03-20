using MediatR;
using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Stores.Queries;

public record GetStoreDetailQuery(Guid PublicId) : IRequest<Result<StoreDetailDto>>;

public class GetStoreDetailQueryHandler : IRequestHandler<GetStoreDetailQuery, Result<StoreDetailDto>>
{
    private readonly IStoreQueryService _queryService;

    public GetStoreDetailQueryHandler(IStoreQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<StoreDetailDto>> Handle(GetStoreDetailQuery request, CancellationToken cancellationToken)
    {
        var store = await _queryService.GetStoreDetailAsync(request.PublicId, cancellationToken);

        if (store == null)
        {
            return Result<StoreDetailDto>.Failure("Không tìm thấy cửa hàng hoặc cửa hàng đã bị xóa.");
        }

        return Result<StoreDetailDto>.Success(store);
    }
}