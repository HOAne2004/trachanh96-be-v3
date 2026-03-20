using MediatR;
using Shared.Application.Models;
using FluentValidation;
using Stores.Application.Interfaces;
using Stores.Application.DTOs.Responses;

namespace Stores.Application.Features.Areas.Queries
{
    public record GetStoreAreasQuery(Guid PublicId) : IRequest<Result<List<AreaResponseDto>>>;

    public class GetStoreAreasQueryValidator : AbstractValidator<GetStoreAreasQuery>
    {
        public GetStoreAreasQueryValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
        }
    }

    public class GetStoreAreasQueryHandler : IRequestHandler<GetStoreAreasQuery, Result<List<AreaResponseDto>>>
    {
        private readonly IStoreQueryService _queryService;
        private readonly IStoreRepository _storeRepository; 

        public GetStoreAreasQueryHandler(IStoreQueryService queryService, IStoreRepository storeRepository)
        {
            _queryService = queryService;
            _storeRepository = storeRepository;
        }

        public async Task<Result<List<AreaResponseDto>>> Handle(GetStoreAreasQuery request, CancellationToken cancellationToken)
        {
            var storeExists = await _storeRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
            if (storeExists == null)
            {
                return Result<List<AreaResponseDto>>.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");
            }

            var areas = await _queryService.GetStoreAreasAsync(request.PublicId, cancellationToken);

            return Result<List<AreaResponseDto>>.Success(areas);
        }
    }
}
