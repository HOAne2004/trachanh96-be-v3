using Catalog.Application.Interfaces;
using Catalog.Domain.Enums;
using MediatR;
using FluentValidation;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.ValueObjects;

namespace Catalog.Application.Features.Products;

public record UpdateProductSizeDto(SizeEnum Size, decimal PriceOverrideAmount, string Currency = "VND");
public record UpdateProductToppingDto(int ToppingId, decimal PriceOverrideAmount, int MaxQuantity = 1, string Currency = "VND");

public record UpdateProductCommand(
    Guid Id, 
    int CategoryId,
    string Name,
    string? Description,
    string? Ingredients,
    string? ImageUrl,
    decimal BasePriceAmount,
    int BasePrepTimeInMinutes,
    string BasePriceCurrency = "VND",
    List<IceLevelEnum>? AllowedIceLevels = null,
    List<SugarLevelEnum>? AllowedSugarLevels = null,
    List<UpdateProductSizeDto>? Sizes = null,
    List<UpdateProductToppingDto>? Toppings = null
) : IRequest<Result>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository productRepository, ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public class UpdateProductSizeDtoValidator : AbstractValidator<UpdateProductSizeDto>
    {
        public UpdateProductSizeDtoValidator()
        {
            RuleFor(x => x.Size).IsInEnum().WithMessage("Kích thước không hợp lệ.");
            RuleFor(x => x.PriceOverrideAmount).GreaterThanOrEqualTo(0).WithMessage("Giá override phải lớn hơn hoặc bằng 0.");
        }
    }
    public class UpdateProductToppingDtoValidator : AbstractValidator<UpdateProductToppingDto>
    {
        public UpdateProductToppingDtoValidator()
        {
            RuleFor(x => x.ToppingId).GreaterThan(0).WithMessage("ID Topping không hợp lệ.");
            RuleFor(x => x.PriceOverrideAmount).GreaterThanOrEqualTo(0).WithMessage("Giá override phải lớn hơn hoặc bằng 0.");
            RuleFor(x => x.MaxQuantity).GreaterThan(0).WithMessage("Số lượng tối đa phải lớn hơn 0.");
        }
    }
    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("ID sản phẩm không hợp lệ.");
            RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("ID danh mục không hợp lệ.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên sản phẩm không được để trống.");
            RuleFor(x => x.BasePriceAmount).GreaterThanOrEqualTo(0).WithMessage("Giá cơ bản phải lớn hơn hoặc bằng 0.");
            RuleFor(x => x.BasePrepTimeInMinutes).GreaterThanOrEqualTo(0).WithMessage("Thời gian chế biến phải lớn hơn hoặc bằng 0.");
            // Validate Sizes
            RuleForEach(x => x.Sizes).SetValidator(new UpdateProductSizeDtoValidator());
            // Validate Toppings
            RuleForEach(x => x.Toppings).SetValidator(new UpdateProductToppingDtoValidator());
        }
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy Product từ DB 
        var product = await _productRepository.GetByPublicIdAsync(request.Id, cancellationToken);
        if (product == null) return Result.Failure("Không tìm thấy sản phẩm.");

        // 2. Validate Category
        if (product.CategoryId != request.CategoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null || !category.IsActive)
                return Result.Failure("Danh mục mới không tồn tại hoặc đã bị ẩn.");

            product.UpdateCategory(request.CategoryId);
        }

        // 3. Check trùng tên (Bỏ qua chính ID hiện tại)
        var isDuplicate = await _productRepository.ExistsByNameAsync(request.Name, request.CategoryId, product.Id, cancellationToken);
        if (isDuplicate) return Result.Failure($"Sản phẩm '{request.Name}' đã tồn tại trong danh mục này.");

        try
        {
            // 4. Update thông tin cơ bản
            product.UpdateDetails(request.Name, request.Description, request.Ingredients, request.ImageUrl, request.BasePrepTimeInMinutes);
            product.UpdateBasePrice(Money.Create(request.BasePriceAmount, request.BasePriceCurrency));
            product.UpdateCustomizations(request.AllowedIceLevels, request.AllowedSugarLevels);

            // 5. THUẬT TOÁN ĐỒNG BỘ SIZE (Sync List)
            var incomingSizes = request.Sizes?.Select(x => x.Size).ToList() ?? new();
            var existingSizes = product.ProductSizes.Select(x => x.Size).ToList();

            // Tìm những Size có trong DB nhưng KHÔNG CÓ trong Request -> Cần Xóa
            var sizesToRemove = existingSizes.Except(incomingSizes).ToList();
            foreach (var s in sizesToRemove) product.RemoveSize(s);

            // Thêm mới hoặc Update những Size từ Request
            if (request.Sizes != null)
            {
                foreach (var s in request.Sizes)
                {
                    product.AddOrUpdateSize(s.Size, Money.Create(s.PriceOverrideAmount, s.Currency));
                }
            }

            // 6. THUẬT TOÁN ĐỒNG BỘ TOPPING (Sync List)
            var incomingToppings = request.Toppings?.Select(x => x.ToppingId).ToList() ?? new();
            var existingToppings = product.ProductToppings.Select(x => x.ToppingId).ToList();

            var toppingsToRemove = existingToppings.Except(incomingToppings).ToList();
            foreach (var t in toppingsToRemove) product.RemoveTopping(t);

            if (request.Toppings != null)
            {
                foreach (var t in request.Toppings)
                {
                    product.AddOrUpdateTopping(t.ToppingId, Money.Create(t.PriceOverrideAmount, t.Currency), t.MaxQuantity);
                }
            }

            // 7. SaveChanges (EF Core sẽ tự động sinh ra các lệnh DELETE, INSERT, UPDATE cho Size và Topping)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            return Result.Failure(ex.Message);
        }
    }
}