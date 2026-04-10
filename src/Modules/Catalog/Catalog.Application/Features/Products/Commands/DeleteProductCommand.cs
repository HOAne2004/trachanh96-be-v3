using Catalog.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Catalog.Application.Features.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest<Result>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy Product từ DB bằng Guid
        var product = await _productRepository.GetByPublicIdAsync(request.Id, cancellationToken);

        if (product == null)
            return Result.Failure("Không tìm thấy sản phẩm hoặc sản phẩm đã bị xóa.");

        // 2. Gọi Domain Behavior để thực hiện logic Xóa mềm
        product.Delete();

        // 3. SaveChanges (EF Core sẽ sinh ra câu lệnh UPDATE, không phải DELETE SQL)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}