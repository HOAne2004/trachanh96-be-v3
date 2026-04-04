using MediatR;
using Shared.Application.Interfaces; 
using Shared.Application.Models;
namespace Shared.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 1. Chuyển quyền cho Handler xử lý logic nghiệp vụ
        var response = await next();

        // 2. Chặn lại ở đây: Nếu response là kiểu Result và báo lỗi (Failure), 
        // thì KHÔNG LƯU gì cả, trả về luôn.
        if (response is Result { IsSuccess: false })
        {
            return response;
        }

        // 3. Nếu Handler chạy thành công, tự động gọi SaveChanges!
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}