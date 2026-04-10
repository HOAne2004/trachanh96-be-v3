/// <summary>
/// [API MIDDLEWARE: BỘ BẮT LỖI TẬP TRUNG TOÀN CẦU]
/// Chức năng: "Tấm khiên" cuối cùng chặn mọi Exception không được handle trong hệ thống, tránh việc rò rỉ stack trace ra bên ngoài và đảm bảo Frontend luôn nhận được JSON.
/// 
/// Cách hoạt động:
/// - Kế thừa IExceptionHandler (Tính năng mới của .NET 8).
/// - Phân loại lỗi: 
///   + ValidationException (từ FluentValidation) -> Trả về HTTP 400 kèm mảng lỗi chi tiết.
///   + DomainException (từ tầng Core) -> Trả về HTTP 400 kèm thông báo vi phạm nghiệp vụ.
///   + UnauthorizedAccessException -> Trả về HTTP 401.
///   + Các lỗi vỡ hệ thống khác (Exception) -> Đóng gói thành HTTP 500 (Internal Server Error).
/// - Sử dụng chuẩn ProblemDetails (RFC 7807) để format cấu trúc JSON trả về.
/// 
/// Sử dụng: Đăng ký trong Program.cs bằng builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
/// Nhờ file này, các Dev làm Handler không cần viết try/catch lặp đi lặp lại nữa.
/// </summary>

using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Exceptions; 

namespace BeverageSystem.Api.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException fluentException)
        {
            problemDetails.Title = "Dữ liệu đầu vào không hợp lệ.";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Extensions.Add("errors", fluentException.Errors.Select(e => e.ErrorMessage));
        }
        // FIX RỦI RO Ở ĐÂY: Chỉ bắt DomainException cho lỗi nghiệp vụ
        else if (exception is DomainException domainException)
        {
            problemDetails.Title = "Lỗi quy tắc nghiệp vụ.";
            // Với lỗi nghiệp vụ, mã 400 (Bad Request) hoặc 422 (Unprocessable Entity) đều chuẩn.
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = domainException.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            problemDetails.Title = "Lỗi xác thực.";
            problemDetails.Status = StatusCodes.Status401Unauthorized;
            problemDetails.Detail = exception.Message;
        }
        else
        {
            // Mọi lỗi hệ thống (chia 0, null reference, lỗi EF Core) sẽ lọt vào đây
            problemDetails.Title = "Lỗi hệ thống nội bộ.";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            // Bật dòng này ở môi trường Dev, nhưng lên Production nên ghi log ẩn đi
            problemDetails.Detail = exception.Message;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}