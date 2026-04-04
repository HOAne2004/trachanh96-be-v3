using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Exceptions; // Thêm using này

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