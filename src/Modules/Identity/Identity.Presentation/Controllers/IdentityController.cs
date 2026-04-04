using Identity.Application.Features.Auth;
using Identity.Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeverageSystem.Api.Controllers;

[ApiController]
[Route("api/identity")]
public class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("users/register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        // 1. Chỉ một dòng duy nhất: Đẩy command vào "ống nước" MediatR
        var userPublicId = await _mediator.Send(command);

        // 2. Trả về kết quả thành công
        return Ok(new
        {
            Message = "Đăng ký tài khoản thành công!",
            UserId = userPublicId
        });
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var authResult = await _mediator.Send(command);
        return Ok(new
        {
            Message = "Đăng nhập thành công!",
            Data = authResult
        });
    }

    [Authorize] 
    [HttpGet("users/me")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var publicId))
        {
            return Unauthorized(new { Message = "Token không hợp lệ." });
        }

        // Đẩy Query xuống MediatR
        var query = new GetProfileQuery(publicId);
        var response = await _mediator.Send(query);

        return Ok(response);
    }

    [HttpPost("users/forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { Message = result });
    }

    [HttpPost("users/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { Message = result });
    }

    [HttpPost("users/verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { Message = result });
    }
}