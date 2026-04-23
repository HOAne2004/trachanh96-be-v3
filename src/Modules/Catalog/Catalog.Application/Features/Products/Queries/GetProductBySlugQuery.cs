using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.ValueObjects;

namespace Catalog.Application.Features.Products.Queries
{
    public record GetProductBySlugQuery(string Slug) : IRequest<Result<ProductDetailDto>>;

    public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, Result<ProductDetailDto>>
    {
        private readonly IProductRepository _productRepository;

        public GetProductBySlugQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductDetailDto>> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
        {
            // 1. Convert string từ request thành Value Object Slug
            // (Sử dụng CreateManual hoặc Create tùy vào cách bạn định nghĩa trong class Slug)
            var slugValueObject = Slug.CreateManual(request.Slug);

            // 2. Truyền Value Object vào Repository
            var product = await _productRepository.GetBySlugAsync(slugValueObject);

            if (product == null)
            {
                return Result<ProductDetailDto>.Failure("Không tìm thấy sản phẩm tương ứng");
            }

            var sizes = product.ProductSizes.Select(s => new ProductSizeDto(
                Size: s.Size.ToString(),
                PriceAmount: s.PriceOverride.Amount,
                Currency: s.PriceOverride.Currency)).ToList();

            var toppings = product.ProductToppings.Select(t => new ProductToppingDto(
                ToppingId: t.ProductId, // Bug tiềm ẩn: Chỗ này có vẻ phải là t.ToppingId thay vì t.ProductId nhé!
                PriceAmount: t.PriceOverride.Amount,
                MaxQuantity: t.MaxQuantity,
                Currency: t.PriceOverride.Currency)).ToList();

            var dto = new ProductDetailDto(
                Id: product.PublicId,
                CategoryId: product.CategoryId,
                Name: product.Name,
                // 3. Mẹo nhỏ: Frontend chỉ đọc được string, nên bạn nhớ gọi .Value
                Slug: product.Slug.Value,
                Description: product.Description,
                Ingredients: product.Ingredients,
                ImageUrl: product.ImageUrl,
                ProductType: product.ProductType.ToString(),
                // 4. BasePrice cũng là Value Object, nhớ gọi .Amount và .Currency
                BasePriceAmount: product.BasePrice.Amount,
                BasePriceCurrency: product.BasePrice.Currency,
                PrepTimeInMinutes: product.BasePrepTimeInMinutes,
                Status: product.Status.ToString(),
                AllowedIceLevels: product.AllowedIceLevels.Select(x => x.ToString()).ToList(),
                AllowedSugarLevels: product.AllowedSugarLevels.Select(x => x.ToString()).ToList(),
                Sizes: sizes,
                Toppings: toppings);

            return Result<ProductDetailDto>.Success(dto);
        }
    }
}
