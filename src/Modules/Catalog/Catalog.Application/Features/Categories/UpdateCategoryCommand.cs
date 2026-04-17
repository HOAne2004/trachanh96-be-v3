using Catalog.Application.Interfaces;
using FluentValidation;
using MediatR;
using Shared.Application.Models;

namespace Catalog.Application.Features.Categories
{
    public record UpdateCategoryCommand(int Id, string Name, int? ParentId, int DisplayOrder) : IRequest<Result<int>>;
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID danh mục không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục không được để trống.")
                .MaximumLength(255).WithMessage("Tên danh mục không được vượt quá 255 ký tự.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");
        }
    }

    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<int>>
    {
        private readonly ICategoryRepository _categoryRepository;
        public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }
        public async Task<Result<int>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy Entity từ DB (EF Core sẽ bắt đầu tracking object này)
            var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
                return (Result<int>)Result.Failure("Danh mục không tồn tại hoặc đã bị xóa.");

            // 2. Kiểm tra trùng lặp tên (Bỏ qua chính nó)
            if (await _categoryRepository.ExistsByNameAsync(request.Name, request.ParentId, request.Id, cancellationToken))
                return (Result<int>)Result.Failure($"Danh mục '{request.Name}' đã tồn tại trong cấp bậc này.");

            // 3. Cập nhật các trường cơ bản thông qua Domain Behaviors
            category.UpdateName(request.Name);
            category.UpdateDisplayOrder(request.DisplayOrder);

            // 4. Xử lý logic chuyển cha-con phức tạp
            if (request.ParentId != category.ParentId)
            {
                if (request.ParentId.HasValue)
                {
                    var parentCategory = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
                    if (parentCategory == null) return (Result<int>)Result.Failure("Danh mục cha không tồn tại.");

                    try
                    {
                        category.SetParent(parentCategory); // Gọi Domain Rule để check tối đa 2 cấp
                    }
                    catch (Exception ex)
                    {
                        return (Result<int>)Result.Failure(ex.Message);
                    }
                }
                else
                {
                    category.RemoveParent(); // Rút ra làm danh mục gốc
                }
            }

            return (Result<int>)Result.Success();
        }
    }
}
