using Catalog.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Catalog.Application.Features.Categories.Commands
{
    public record ToggleCategoryStatusCommand(int Id, bool IsActive) : ICommand<Result<bool>>;
    public class ToggleCategoryStatusHandler : IRequestHandler<ToggleCategoryStatusCommand, Result<bool>>
    {
        private readonly ICategoryRepository _category;

        public ToggleCategoryStatusHandler(ICategoryRepository category)
        {
            _category = category;
        }

        public async Task<Result<bool>> Handle(ToggleCategoryStatusCommand request, CancellationToken cancellationToken)
        {
            var cate = await _category.GetByIdAsync(request.Id, cancellationToken);
            if(cate == null)
            {
                return Result<bool>.Failure("Danh mục không tồn tại.");
            }

            cate.ToggleActiveStatus(request.IsActive);
            return Result<bool>.Success(cate.IsActive);
        }
    }
}
