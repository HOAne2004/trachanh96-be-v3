using MediatR;
using Shared.Application.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;

namespace Catalog.Application.Features.Categories.Queries
{
    public record GetCategoriesQuery() : IRequest<Result<List<CategoryDto>>>;

    public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, Result<List<CategoryDto>>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetCategoriesHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetAllAsync();
            if (categories == null || !categories.Any())
            {
                return Result<List<CategoryDto>>.Failure("Không tìm thấy danh mục nào");
            }
            var categoryDtos = categories.Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Slug.Value,
                c.ParentId,
                c.DisplayOrder,
                c.IsActive
            )).ToList();

            return Result<List<CategoryDto>>.Success(categoryDtos);
        }
    }
}