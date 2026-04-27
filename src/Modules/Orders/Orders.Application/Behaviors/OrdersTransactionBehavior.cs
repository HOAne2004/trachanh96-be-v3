using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Orders.Application.Behaviors
{
    public class OrdersTransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    {
        private readonly IOrdersUnitOfWork _unitOfWork;

        public OrdersTransactionBehavior(IOrdersUnitOfWork unitOfWork)
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
}
