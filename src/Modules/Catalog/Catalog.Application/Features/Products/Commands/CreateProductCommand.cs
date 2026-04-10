using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Shared.Domain.Enums;
using FluentValidation;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.ValueObjects;

namespace Catalog.Application.Features.Products.Commands;

// DTO cho Size
public record CreateProductSizeDto(
    SizeEnum Size,
    decimal PriceOverrideAmount,
    string Currency = "VND"
);

// DTO cho Topping
public record CreateProductToppingDto(
    int ToppingId,
    decimal PriceOverrideAmount,
    int MaxQuantity = 1,
    string Currency = "VND"
);

public record CreateProductCommand(
    int CategoryId,
    string Name,
    string? Description,
    string? Ingredients,
    string? ImageUrl,
    ProductTypeEnum ProductType,
    decimal BasePriceAmount,
    int BasePrepTimeInMinutes,
    string BasePriceCurrency = "VND",
    List<IceLevelEnum>? AllowedIceLevels = null,
    List<SugarLevelEnum>? AllowedSugarLevels = null,
    List<CreateProductSizeDto>? Sizes = null,
    List<CreateProductToppingDto>? Toppings = null
) : IRequest<Result<Guid>>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Vui lòng chọn Danh mục.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Tên không được chỉ chứa khoảng trắng.")
            .MaximumLength(255).WithMessage("Tên sản phẩm không quá 255 ký tự.");

        RuleFor(x => x.BasePriceAmount).GreaterThanOrEqualTo(0).WithMessage("Giá cơ sở không được âm.");
        RuleFor(x => x.BasePrepTimeInMinutes).GreaterThanOrEqualTo(0).WithMessage("Thời gian chuẩn bị không được âm.");

        // Validate List Size
        When(x => x.Sizes != null && x.Sizes.Any(), () =>
        {
            RuleFor(x => x.Sizes)
                .Must(sizes => sizes!.Select(s => s.Size).Distinct().Count() == sizes!.Count)
                .WithMessage("Danh sách kích cỡ không được chứa các Size trùng lặp.");

            RuleForEach(x => x.Sizes).ChildRules(size =>
            {
                size.RuleFor(s => s.Size).IsInEnum().WithMessage("Kích cỡ không hợp lệ.");
                size.RuleFor(s => s.PriceOverrideAmount).GreaterThanOrEqualTo(0).WithMessage("Giá Size không được âm.");
            });
        });

        // Validate List Topping
        When(x => x.Toppings != null && x.Toppings.Any(), () =>
        {
            RuleFor(x => x.Toppings)
                .Must(toppings => toppings!.Select(t => t.ToppingId).Distinct().Count() == toppings!.Count)
                .WithMessage("Danh sách Topping không được chứa các Topping trùng lặp.");

            RuleForEach(x => x.Toppings).ChildRules(topping =>
            {
                topping.RuleFor(t => t.ToppingId).GreaterThan(0).WithMessage("ToppingId không hợp lệ.");
                topping.RuleFor(t => t.PriceOverrideAmount).GreaterThanOrEqualTo(0).WithMessage("Giá Topping không được âm.");
                topping.RuleFor(t => t.MaxQuantity).InclusiveBetween(1, 5).WithMessage("Số lượng tối đa từ 1 đến 5.");
            });
        });
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateProductCommandHandler(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null) return Result<Guid>.Failure("Danh mục không tồn tại.");

        if (!category.IsActive) return Result<Guid>.Failure("Không thể thêm sản phẩm vào danh mục đang bị vô hiệu hóa.");

        var isDuplicateName = await _productRepository.ExistsByNameAsync(request.Name, request.CategoryId, null, cancellationToken);
        if (isDuplicateName)
        {
            return Result<Guid>.Failure($"Sản phẩm '{request.Name}' đã tồn tại trong danh mục này.");
        }

        try
        {
            var basePrice = Money.Create(request.BasePriceAmount, request.BasePriceCurrency);

            var publicId = Guid.NewGuid();
            var product = new Product(
                publicId: publicId,
                categoryId: request.CategoryId,
                name: request.Name,
                productType: request.ProductType,
                basePrice: basePrice,
                basePrepTimeInMinutes: request.BasePrepTimeInMinutes
            );

            if (!string.IsNullOrEmpty(request.Description) || !string.IsNullOrEmpty(request.Ingredients) || !string.IsNullOrEmpty(request.ImageUrl))
            {
                product.UpdateDetails(request.Name, request.Description, request.Ingredients, request.ImageUrl, request.BasePrepTimeInMinutes);
                product.UpdateCustomizations(request.AllowedIceLevels, request.AllowedSugarLevels);
            }

            if (request.Sizes != null)
            {
                foreach (var sizeDto in request.Sizes)
                {
                    product.AddOrUpdateSize(sizeDto.Size, Money.Create(sizeDto.PriceOverrideAmount, sizeDto.Currency));
                }
            }

            if (request.Toppings != null)
            {
                foreach (var toppingDto in request.Toppings)
                {
                    product.AddOrUpdateTopping(toppingDto.ToppingId, Money.Create(toppingDto.PriceOverrideAmount, toppingDto.Currency), toppingDto.MaxQuantity);
                }
            }

            _productRepository.Add(product);

            return Result<Guid>.Success(product.PublicId);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure($"Lỗi dữ liệu đầu vào: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure($"Lỗi quy tắc nghiệp vụ: {ex.Message}");
        }
    }
}