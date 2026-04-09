/// <summary>
/// [PIPELINE BEHAVIOR: QUẢN LÝ TRANSACTION TỰ ĐỘNG]
/// Chức năng: Tự động gom các thao tác Database vào một Transaction (All-or-Nothing).
/// Cách hoạt động:
/// - Cho phép Handler thực thi logic nghiệp vụ (thêm, sửa, xóa vào DbContext).
/// - Nhận kết quả từ Handler. Nếu kết quả là LỖI (Result.IsSuccess == false), KHÔNG GỌI SaveChanges.
/// - Nếu kết quả THÀNH CÔNG, tự động gọi _unitOfWork.SaveChangesAsync() để commit dữ liệu vào DB.
/// Sử dụng: Áp dụng tự động cho mọi Request. Giúp loại bỏ code Boilerplate (SaveChanges) rải rác trong các Handlers.
/// </summary>

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