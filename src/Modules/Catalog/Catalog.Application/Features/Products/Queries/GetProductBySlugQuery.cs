using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.ValueObjects;

namespace Catalog.Application.Features.Products.Queries
{
    public record GetProductBySlugQuery(Slug Slug) : IRequest<Result<ProductDetailDto>>;

    public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, Result<ProductDetailDto>>
    {
        private readonly IProductRepository _productRepository;

        public GetProductBySlugQueryHandler (IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductDetailDto>> Handle (GetProductBySlugQuery request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetBySlugAsync (request.Slug);

            if (product == null)
            {
                return Result<ProductDetailDto>.Failure("Không tìm thấy sản phẩm tương ứng");
            }

            var sizes = product.ProductSizes.Select(s => new ProductSizeDto(
                Size: s.Size.ToString(),
                PriceAmount: s.PriceOverride.Amount,
                Currency: s.PriceOverride.Currency)).ToList();

            var toppings = product.ProductToppings.Select(t => new ProductToppingDto(
                ToppingId: t.ProductId,
                PriceAmount: t.PriceOverride.Amount,
                MaxQuantity: t.MaxQuantity,
                Currency: t.PriceOverride.Currency)).ToList();

            var dto = new ProductDetailDto(
                Id: product.PublicId,
                CategoryId: product.CategoryId,
                Name: product.Name,
                Slug: product.Slug,
                Description: product.Description,
                Ingredients: product.Ingredients,
                ImageUrl: product.ImageUrl,
                ProductType: product.ProductType.ToString(),
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
