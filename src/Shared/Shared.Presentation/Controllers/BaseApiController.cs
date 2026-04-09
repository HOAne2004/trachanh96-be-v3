using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Models;

namespace Shared.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        private ISender? _sender;

        // Tự động inject ISender, các Controller con không cần khai báo constructor lằng nhằng nữa
        protected ISender Mediator => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

        // Hàm bọc cho Result trả về Dữ liệu (Ví dụ: Id, List object)
        protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "Thành công")
        {
            if (result.IsFailure)
            {
                return BadRequest(new ErrorResponse("BAD_REQUEST", result.Error));
            }

            // Tự động bọc vào ApiResponse mà Frontend đang chờ đợi
            var response = new ApiResponse<T>
            {
                Data = result.Value, // Không cần Reflection!
                Message = successMessage,
                Success = true,
                Status = 200
            };
            return Ok(response);
        }

        // Hàm bọc cho Result KHÔNG trả về Dữ liệu (Ví dụ: Delete xong là thôi)
        protected IActionResult HandleResult(Result result, string successMessage = "Thành công")
        {
            if (result.IsFailure)
            {
                return BadRequest(new ErrorResponse("BAD_REQUEST", result.Error));
            }

            var response = new ApiResponse<object>
            {
                Data = null,
                Message = successMessage,
                Success = true,
                Status = 200
            };
            return Ok(response);
        }
    }
}
