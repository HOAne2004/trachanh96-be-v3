using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BeverageSystem.Api.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Khởi tạo khung JSON trả về chuẩn REST API (ProblemDetails)
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        // 1. Nếu là lỗi dữ liệu đầu vào (FluentValidation quăng ra từ Behavior)
        if (exception is ValidationException fluentException)
        {
            problemDetails.Title = "Dữ liệu đầu vào không hợp lệ.";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            // Gom danh sách lỗi trả về cho Frontend hiển thị
            problemDetails.Extensions.Add("errors", fluentException.Errors.Select(e => e.ErrorMessage));
        }
        // 2. Nếu là lỗi nghiệp vụ (Domain Exception, Email trùng, v.v.)
        else if (exception is ArgumentException || exception is InvalidOperationException)
        {
            problemDetails.Title = "Lỗi nghiệp vụ.";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = exception.Message;
        }
        // 3. Nếu là lỗi sai mật khẩu / email
        else if (exception is UnauthorizedAccessException)
        {
            problemDetails.Title = "Lỗi xác thực.";
            problemDetails.Status = StatusCodes.Status401Unauthorized;
            problemDetails.Detail = exception.Message;
        }
        // 4. Nếu là lỗi sập hệ thống (NullReference, Database sập...)
        else
        {
            problemDetails.Title = "Lỗi hệ thống nội bộ.";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Detail = exception.Message; // Thực tế khi lên Production nên ẩn dòng này đi
        }

        // Ghi đè mã Status Code và xuất JSON ra
        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}