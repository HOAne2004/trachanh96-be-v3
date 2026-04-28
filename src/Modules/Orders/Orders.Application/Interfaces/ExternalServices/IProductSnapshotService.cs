using Shared.Domain.Enums;

namespace Orders.Application.Interfaces.ExternalServices;

public record ProductSnapshotDto(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    SizeEnum Size,
    decimal Price,
    string Currency);

public record ToppingSnapshotDto(
    Guid ToppingId,
    string ToppingName,
    string? ImageUrl,
    decimal Price,
    string Currency);
public interface IProductSnapshotService
{
    Task<List<ProductSnapshotDto>> GetProductSnapshotsAsync(
        IEnumerable<(Guid ProductId, SizeEnum Size)> productSizes,
        CancellationToken cancellationToken);

    Task<List<ToppingSnapshotDto>> GetToppingSnapshotsAsync(
        IEnumerable<Guid> toppingIds,
        CancellationToken cancellationToken);
}