using MediatR;
using Shared.Application.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;

namespace Catalog.Application.Features.Categories.Queries
{
    public record GetCategoryByIdQuery(int Id) : IRequest<Result<CategoryDto>>;

    public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetCategoryByIdHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
            {
                return Result<CategoryDto>.Failure("Không tìm thấy danh mục");
            }

            var categoryDto = new CategoryDto(
                category.Id,
                category.Name,
                category.Slug.Value,
                category.ParentId,
                category.DisplayOrder,
                category.IsActive
            );

            return Result<CategoryDto>.Success(categoryDto);
        }
    }
}