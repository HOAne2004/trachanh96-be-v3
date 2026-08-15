using Identity.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Presentation.Controllers;

namespace Identity.Presentation.Controllers;

[Route("api/identity/auth")]
public class AuthController : BaseApiController
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Đăng ký tài khoản thành công! Vui lòng kiểm tra email để xác thực.");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var secureCommand = command with { DeviceName = GetDeviceName(), IpAddress = GetClientIpAddress() };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Đăng nhập thành công!");
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var secureCommand = command with { DeviceName = GetDeviceName(), IpAddress = GetClientIpAddress() };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Làm mới phiên thành công!");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Đăng xuất thành công.");
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
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

    // ==========================================================
    // Helper: lấy IP/Device thật của client - cần app.UseForwardedHeaders() đã cấu hình đúng
    // trong Program.cs khi chạy sau reverse proxy (xem ghi chú Program.cs bên dưới), nếu không
    // giá trị này luôn là IP của proxy, không phải IP thật của người dùng.
    // ==========================================================
    private string GetDeviceName() => Request.Headers.UserAgent.ToString();

    private string GetClientIpAddress()
    {
        var ip = HttpContext.Connection.RemoteIpAddress;
        // Một số môi trường (Docker/localhost IPv6 dual-stack) trả IPv4-mapped-to-IPv6
        // dạng "::ffff:172.18.0.1" - chuẩn hóa về IPv4 thuần cho dễ đọc/so sánh khi audit.
        if (ip != null && ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }
        return ip?.ToString() ?? "Unknown";
    }
}