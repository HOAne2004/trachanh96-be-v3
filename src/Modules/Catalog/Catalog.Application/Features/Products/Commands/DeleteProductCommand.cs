using Catalog.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Catalog.Application.Features.Products.Commands;

public record DeleteProductCommand(Guid Id) : ICommand<Result<Guid>>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result<Guid>>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<Guid>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy Product từ DB bằng Guid
        var product = await _productRepository.GetByPublicIdAsync(request.Id, cancellationToken);

        if (product == null)
            return Result<Guid>.Failure("Không tìm thấy sản phẩm hoặc sản phẩm đã bị xóa.");

        // 2. Gọi Domain Behavior để thực hiện logic Xóa mềm
        product.Delete();
        return Result<Guid>.Success(product.PublicId);
    }
}