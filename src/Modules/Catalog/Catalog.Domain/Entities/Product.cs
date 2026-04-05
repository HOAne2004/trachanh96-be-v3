using Catalog.Domain.Enums;
using Shared.Domain.Enums;
using Shared.Domain.ValueObjects;
using Shared.Domain.SeedWork;
using Shared.Domain.Interfaces;

namespace Catalog.Domain.Entities;

public class Product : AggregateRoot<int>, IAuditableEntity, ISoftDeletableEntity
{
    public Guid PublicId { get; private set; }
    public int CategoryId { get; private set; }
    public string Name { get; private set; }
    public Slug Slug { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Description { get; private set; }
    public string? Ingredients { get; private set; }
    public ProductTypeEnum ProductType { get; private set; }
    public Money BasePrice { get; private set; }
    public int BasePrepTimeInMinutes { get; private set; }
    public ProductStatusEnum Status { get; private set; }

    private readonly List<ProductSize> _productSizes = new();
    private readonly List<ProductTopping> _productToppings = new();
    public IReadOnlyCollection<ProductSize> ProductSizes => _productSizes.AsReadOnly();
    public IReadOnlyCollection<ProductTopping> ProductToppings => _productToppings.AsReadOnly();

    public List<IceLevelEnum> AllowedIceLevels { get; private set; } = new();
    public List<SugarLevelEnum> AllowedSugarLevels { get; private set; } = new();
    public double TotalRating { get; private set; } = 0;
    public int TotalSold { get; private set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    protected Product()
    {
        Name = null!;
        Slug = null!;
        BasePrice = null!;
    }

    public Product(Guid publicId, int categoryId, string name, ProductTypeEnum productType, Money basePrice, int basePrepTimeInMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên sản phẩm không được trống.");
        if (basePrepTimeInMinutes < 0)
            throw new ArgumentException("Thời gian chế biến không thể âm.");

        PublicId = publicId;
        CategoryId = categoryId;
        Name = name.Trim();
        Slug = Slug.Create(Name);
        ProductType = productType;
        BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
        BasePrepTimeInMinutes = basePrepTimeInMinutes;

        Status = ProductStatusEnum.Draft;
        IsDeleted = false;
    }

    public void UpdateDetails(string name, string? description, string? ingredients, string? imageUrl, int basePrepTimeInMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên sản phẩm không được trống.");

        if (basePrepTimeInMinutes < 0)
            throw new ArgumentException("Thời gian chế biến không thể âm.");

        Name = name.Trim();
        Slug = Slug.Create(Name);
        Description = description;
        Ingredients = ingredients;
        ImageUrl = imageUrl;
        BasePrepTimeInMinutes = basePrepTimeInMinutes;
    }

    public void UpdateBasePrice(Money newPrice)
    {
        BasePrice = newPrice ?? throw new ArgumentNullException(nameof(newPrice), "Giá cơ sở không được để trống.");
    }

    public void AddOrUpdateSize(SizeEnum size, Money priceOverride)
    {
        if (priceOverride == null) throw new ArgumentNullException(nameof(priceOverride));

        var existingSize = _productSizes.FirstOrDefault(s => s.Size == size);

        if (existingSize != null)
        {
            existingSize.UpdatePrice(priceOverride);
        }
        else
        {
            _productSizes.Add(new ProductSize(this.Id, size, priceOverride));
        }
    }

    public void RemoveSize(SizeEnum size)
    {
        if (_productSizes.Count <= 1)
            throw new InvalidOperationException("Sản phẩm bắt buộc phải có ít nhất 1 kích cỡ.");

        var rmsize = _productSizes.FirstOrDefault(s => s.Size == size);
        if (rmsize != null)
        {
            _productSizes.Remove(rmsize);
        }
    }

    public void AddOrUpdateTopping(int toppingId, Money priceOverride, int maxQuantity)
    {
        if (priceOverride == null) throw new ArgumentNullException(nameof(priceOverride));

        // ĐÃ SỬA: Rule bảo vệ (Không cho phép cấu hình quá 5 phần Topping cùng loại cho 1 món)
        if (maxQuantity < 1 || maxQuantity > 5)
            throw new ArgumentException("Số lượng Topping tối đa cho phép cấu hình là từ 1 đến 5.");

        if (_productToppings.Count >= 10 && !_productToppings.Any(t => t.ToppingId == toppingId))
            throw new InvalidOperationException("Một sản phẩm không được cấu hình quá 10 loại Topping.");

        var existingTopping = _productToppings.FirstOrDefault(t => t.ToppingId == toppingId);
        if (existingTopping != null)
        {
            existingTopping.Update(priceOverride, maxQuantity);
        }
        else
        {
            _productToppings.Add(new ProductTopping(this.Id, toppingId, priceOverride, maxQuantity));
        }
    }

    public void RemoveTopping(int toppingId)
    {
        var topping = _productToppings.FirstOrDefault(t => t.ToppingId == toppingId);
        if (topping != null)
        {
            _productToppings.Remove(topping);
        }
    }

    public void ChangeStatus(ProductStatusEnum newStatus)
    {
        Status = newStatus;
    }

    public void Publish()
    {
        if (Status == ProductStatusEnum.Archived)
            throw new InvalidOperationException("Không thể mở bán lại một sản phẩm đã bị đưa vào Lưu trữ (Archived).");

        if (_productSizes.Count == 0)
            throw new InvalidOperationException("Sản phẩm phải được cấu hình ít nhất 1 kích cỡ (Size) trước khi mở bán.");

        Status = ProductStatusEnum.Active;
    }

    public void MarkAsComingSoon()
    {
        if (Status == ProductStatusEnum.Archived)
            throw new InvalidOperationException("Sản phẩm đã lưu trữ không thể chuyển thành trạng thái Sắp ra mắt.");

        Status = ProductStatusEnum.ComingSoon;
    }

    public void Deactivate()
    {
        if (Status == ProductStatusEnum.Archived)
            throw new InvalidOperationException("Sản phẩm đã lưu trữ không thể thay đổi trạng thái.");

        Status = ProductStatusEnum.Inactive;
    }

    public void Archive()
    {
        Status = ProductStatusEnum.Archived;
    }

    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        Status = ProductStatusEnum.Archived; 
    }

    public void IncrementSoldQuantity(int quantity)
    {
        if (quantity > 0) TotalSold += quantity;
    }

    public void UpdateRating(double newAverageRating)
    {
        if (newAverageRating >= 0 && newAverageRating <= 5)
            TotalRating = newAverageRating;
    }
    public void UpdateCustomizations(List<IceLevelEnum>? iceLevels, List<SugarLevelEnum>? sugarLevels)
    {
        AllowedIceLevels = iceLevels?.Distinct().ToList() ?? new List<IceLevelEnum>();
        AllowedSugarLevels = sugarLevels?.Distinct().ToList() ?? new List<SugarLevelEnum>();
    }

    public void UpdateCategory(int newCategoryId)
    {
        CategoryId = newCategoryId;
    }
}