using Identity.Application.Features.Auth.Commands;
using Identity.Application.Features.Users.Commands;
using Identity.Application.Features.Users.Queries; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Shared.Application.Models;
using Shared.Presentation.Controllers;
using System.Security.Claims;

namespace Identity.Presentation.Controllers;

[Route("api/identity/users/me")]
[Authorize]
public class UsersController : BaseApiController
{
    // Hàm Helper để tái sử dụng việc lấy PublicId từ Token
    private Guid GetCurrentUserPublicId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var publicId) ? publicId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var publicId = GetCurrentUserPublicId();
        if (publicId == Guid.Empty)
            return Unauthorized(new ErrorResponse("INVALID_TOKEN", "Token không hợp lệ."));

        var result = await Mediator.Send(new GetProfileQuery(publicId));
        return HandleResult(result);
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Lấy UserPublicId từ claims (đã được set trong token khi login)
        var userPublicIdClaim = User.FindFirst("PublicId")?.Value
                                 ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userPublicIdClaim) || !Guid.TryParse(userPublicIdClaim, out Guid userPublicId))
        {
            return BadRequest(new { message = "Không thể xác định người dùng từ token" });
        }

        var command = new LogoutCommand(userPublicId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
    {
        // Gắn Id từ Token vào để bảo mật IDOR
        var secureCommand = command with { UserPublicId = GetCurrentUserPublicId() };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Cập nhật thông tin cá nhân thành công!");
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var secureCommand = command with { UserPublicId = GetCurrentUserPublicId() };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Đổi mật khẩu thành công!");
    }

    [HttpPut("email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailCommand command)
    {
        var secureCommand = command with { UserPublicId = GetCurrentUserPublicId() };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Đổi email thành công!");
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] Application.Features.Users.Commands.VerifyEmailCommand command)
    {
        var secureCommand = command with { UserPublicId = GetCurrentUserPublicId() };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result);
    }


}