using Identity.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Presentation.Controllers;

namespace Identity.Presentation.Controllers;

[Route("api/identity/auth")]
public class AuthController : BaseApiController
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Đăng ký tài khoản thành công! Vui lòng kiểm tra email để xác thực.");
    }

    [HttpPost("login")]
    [AllowAnonymous] 
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        // Lấy thông tin thiết bị và IP từ HTTP Context
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Gắn thêm vào Command (dùng record 'with' expression nếu DTO cho phép, hoặc bọc lại)
        var secureCommand = command with { DeviceName = userAgent, IpAddress = ipAddress };

        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Đăng nhập thành công!");
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var secureCommand = command with { DeviceName = userAgent, IpAddress = ipAddress };

        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Làm mới phiên thành công!");
    }

    [HttpPost("logout")]
    [Authorize] // Phải có token thì mới được logout
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Đăng xuất thành công.");
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]

    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}