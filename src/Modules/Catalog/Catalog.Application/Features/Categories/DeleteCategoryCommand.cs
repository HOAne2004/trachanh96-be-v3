using Catalog.Application.Interfaces;
using FluentValidation;
using MediatR;
using Shared.Application.Models;

namespace Catalog.Application.Features.Categories
{
    public record DeleteCategoryCommand(int Id) : IRequest<Result<int>>;
    public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
    {
        public DeleteCategoryCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID danh mục không hợp lệ.");
        }
    }

    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<int>>
    {
        private readonly ICategoryRepository _categoryRepository;
        public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }
        public async Task<Result<int>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy Entity từ DB
            var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
                return Result<int>.Failure("Danh mục không tồn tại hoặc đã bị xóa.");
            // 2. Kiểm tra nếu có danh mục con thì không cho xóa
            if (await _categoryRepository.HasChildrenAsync(request.Id, cancellationToken))
                return Result<int>.Failure("Không thể xóa danh mục này vì nó có danh mục con. Vui lòng xóa danh mục con trước");
            // 3. Xóa mềm (Soft Delete)
            category.IsDeleted = true;
            return Result<int>.Success(category.Id);
        }
    }
}
