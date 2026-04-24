using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Catalog.Application.Features.Products.Queries
{
    public record GetProductByIdQuery(Guid productId, Guid? StoreId = null) : IRequest<Result<ProductDetailDto>>;

    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
    {
        private readonly IProductRepository _productRepository;
        public GetProductByIdQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductDetailDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByPublicIdAsync(request.productId, cancellationToken);

            if (product == null)
            {
                return Result<ProductDetailDto>.Failure("Không tìm thấy món");
            }
            var storeInfo = request.StoreId.HasValue
                ? product.StoreProducts.FirstOrDefault(sp => sp.StoreId == request.StoreId.Value)
                : null;

            var sizes = product.ProductSizes.Select(s => new ProductSizeDto(
                Size: s.Size.ToString(),
                PriceAmount: s.PriceOverride.Amount,
                Currency: s.PriceOverride.Currency)).ToList();

            var toppings = product.ProductToppings.Select(t => new ProductToppingDto(
                ToppingId: t.ToppingId, 
                Name: t.Topping?.Name ?? "Topping chưa rõ tên", 
                ImageUrl: t.Topping?.ImageUrl,
                PriceAmount: t.PriceOverride.Amount,
                MaxQuantity: t.MaxQuantity,
                Currency: t.PriceOverride.Currency)).ToList();

            var dto = new ProductDetailDto(
                  Id: product.PublicId,
                  CategoryId: product.CategoryId,
                  StoreId: request.StoreId, // Trả về chính StoreId khách đang xem
                  Name: product.Name,
                  Slug: product.Slug.Value,
                  Description: product.Description,
                  Ingredients: product.Ingredients,
                  ImageUrl: product.ImageUrl,
                  ProductType: product.ProductType.ToString(),

                  // 3. LOGIC GIÁ BÁN: Ưu tiên giá của quán, không có thì lấy giá gốc
                  BasePriceAmount: storeInfo?.PriceOverride ?? product.BasePrice.Amount,
                  BasePriceCurrency: product.BasePrice.Currency,

                  PrepTimeInMinutes: product.BasePrepTimeInMinutes,

                  // 4. LOGIC TRẠNG THÁI: Quán báo hết hàng thì trả về OutOfStock
                  Status: (storeInfo != null && !storeInfo.IsAvailable) ? "OutOfStock" : product.Status.ToString(),

                  AllowedIceLevels: product.AllowedIceLevels.Select(x => x.ToString()).ToList(),
                  AllowedSugarLevels: product.AllowedSugarLevels.Select(x => x.ToString()).ToList(),
                  Sizes: sizes,
                  Toppings: toppings,
                  TotalSold: product.TotalSold,
                  TotalRating: product.TotalRating,
                  PublishedAt: product.PublishedAt,
                  CreatedAt: product.CreatedAt);

            return Result<ProductDetailDto>.Success(dto);
        }
    }
}
