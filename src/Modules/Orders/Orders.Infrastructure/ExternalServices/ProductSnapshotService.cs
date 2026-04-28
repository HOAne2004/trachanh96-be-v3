using Orders.Application.Interfaces.ExternalServices;
using Shared.Domain.Enums;

namespace Orders.Infrastructure.ExternalServices;

public class ProductSnapshotService : IProductSnapshotService
{
    public Task<List<ProductSnapshotDto>> GetProductSnapshotsAsync(
        IEnumerable<(Guid ProductId, SizeEnum Size)> productSizes,
        CancellationToken cancellationToken)
    {
        // Trả về dữ liệu giả để Test FE
        var result = productSizes.Select(p => new ProductSnapshotDto(
            p.ProductId,
            "Trà Chanh Đào Tiên (Mock)",
            "https://example.com/mock-image.png", // DỮ LIỆU GIẢ
            p.Size,
            p.Size == SizeEnum.M ? 25000 : 35000,
            "VND"
        )).ToList();

        return Task.FromResult(result);
    }

    public Task<List<ToppingSnapshotDto>> GetToppingSnapshotsAsync(
        IEnumerable<Guid> toppingIds,
        CancellationToken cancellationToken)
    {
        // Trả về dữ liệu giả Topping
        var result = toppingIds.Select(id => new ToppingSnapshotDto(
            id,
            "Trân Châu Trắng (Mock)",
            "https://example.com/mock-image.png", // DỮ LIỆU GIẢ
            10000,
            "VND"
        )).ToList();

        return Task.FromResult(result);
    }
}