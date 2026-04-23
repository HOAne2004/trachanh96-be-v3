using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Catalog.Application.Features.Categories.Commands
{
    public record CreateCategoryCommand(string Name, int? ParentId, int DisplayOrder) : ICommand<Result<int>>;

    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục không được để trống.")
                .MaximumLength(255).WithMessage("Tên danh mục không được vượt quá 255 ký tự.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");
        }
    }

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<int>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public CreateCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<int>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra trùng lặp (Logic Application) 
            if (await _categoryRepository.ExistsByNameAsync(request.Name, request.ParentId, null, cancellationToken))
            {
                return Result<int>.Failure($"Danh mục '{request.Name}' đã tồn tại trong cấp bậc này.");
            }

            // 2. Khởi tạo Entity (Domain Logic đã tự handle Slug bên trong)
            // Mẹo: Ta không truyền ParentId vào constructor để lát nữa ép nó phải đi qua hàm SetParent (để check rule tối đa 2 cấp)
            var category = new Category(request.Name, null, request.DisplayOrder);

            // 3. Xử lý Logic Danh mục Cha - Con
            if (request.ParentId.HasValue)
            {
                var parentCategory = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
                if (parentCategory == null)
                {
                    return Result<int>.Failure("Danh mục cha không tồn tại.");
                }

                try
                {
                    // Gọi Domain Behavior để Entity tự bảo vệ mình (Check không cho quá 2 cấp)
                    category.SetParent(parentCategory);
                }
                catch (Exception ex)
                {
                    // Bắt lỗi Domain Exception ném ra từ Entity
                    return Result<int>.Failure(ex.Message);
                }
            }

            // 4. Ủy quyền cho Repository (Chỉ tracking in-memory)
            _categoryRepository.Add(category);

            return Result<int>.Success(category.Id);
        }
    }
}
