using Identity.Application.Features.Auth.Commands;
using Identity.Application.Features.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;
using System.Security.Claims;

namespace Identity.Presentation.Controllers;

[Route("api/identity")]
public class IdentityController : BaseApiController 
{

    [HttpPost("users/register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        // Giả định RegisterUserCommand trả về Result<Guid>
        var result = await Mediator.Send(command);
        return HandleResult(result, "Đăng ký tài khoản thành công!");
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        // Giả định LoginCommand trả về Result<AuthResultDto>
        var result = await Mediator.Send(command);
        return HandleResult(result, "Đăng nhập thành công!");
    }

    [Authorize]
    [HttpGet("users/me")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var publicId))
        {
            // Trả về ErrorResponse chuẩn hóa thay vì object vô danh
            return Unauthorized(new ErrorResponse("INVALID_TOKEN", "Token không hợp lệ hoặc đã hết hạn."));
        }

        // Giả định GetProfileQuery trả về Result<UserProfileDto>
        var result = await Mediator.Send(new GetProfileQuery(publicId));
        return HandleResult(result);
    }

    [HttpPost("users/forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        // Giả định ForgotPasswordCommand trả về Result (không có T)
        var result = await Mediator.Send(command);
        return HandleResult(result, "Đã gửi email khôi phục mật khẩu. Vui lòng kiểm tra hộp thư.");
    }

    [HttpPost("users/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Khôi phục mật khẩu thành công!");
    }

    [HttpPost("users/verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Xác thực email thành công!");
    }
}