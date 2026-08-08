using Identity.Application.Features.Addresses.Commands;
using Identity.Application.Features.Addresses.Queries;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Features.Auth.Queries;
using Identity.Application.Features.Users.Commands;
using Identity.Application.Features.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Presentation.Controllers;

namespace Identity.Presentation.Controllers;

[Route("api/identity/users/me")]
[Authorize] // Cổng gác cơ bản: Yêu cầu đăng nhập
public class UsersController : BaseApiController
{
    // --- PROFILE MANAGEMENT ---

    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await Mediator.Send(new GetProfileQuery()); // Không cần truyền ID!
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileCommand command)
    {
        var result = await Mediator.Send(command); // Không cần truyền ID!
        return HandleResult(result);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut("email")]
    public async Task<IActionResult> RequestChangeMyEmail([FromBody] RequestChangeEmailCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] ConfirmChangeEmailCommand command)
    {
        // Controller không cần phải chế biến ID nữa, Handler sẽ tự lo qua ICurrentUser
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
    // --- SESSION MANAGEMENT ---

    [HttpGet("sessions")]
    public async Task<IActionResult> GetMySessions()
    {
        var result = await Mediator.Send(new GetMyActiveSessionsQuery());
        return HandleResult(result);
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeDeviceSession(Guid sessionId)
    {
        var result = await Mediator.Send(new RevokeDeviceSessionCommand(sessionId));
        return HandleResult(result, "Đã đăng xuất khỏi thiết bị đã chọn.");
    }

    [HttpDelete("sessions")]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var result = await Mediator.Send(new RevokeAllSessionsCommand());
        return HandleResult(result, "Đã đăng xuất khỏi tất cả các thiết bị.");
    }

    // ==========================================================
    // --- SỔ ĐỊA CHỈ (ADDRESS BOOK MANAGEMENT) ---
    // ==========================================================

    [HttpGet("addresses")]
    public async Task<IActionResult> GetMyAddresses()
    {
        var result = await Mediator.Send(new GetMyAddressesQuery());
        return HandleResult(result);
    }

    [HttpGet("addresses/{addressId:guid}")]
    public async Task<IActionResult> GetMyAddressById(Guid addressId)
    {
        var result = await Mediator.Send(new GetAddressByIdQuery(addressId));
        return HandleResult(result);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result, "Thêm địa chỉ giao hàng thành công.");
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] UpdateAddressCommand command)
    {
        // Gắn ID từ URL vào Command để bảo đảm không bị lệch dữ liệu
        var secureCommand = command with { AddressId = addressId };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result);
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId)
    {
        var result = await Mediator.Send(new DeleteAddressCommand(addressId));
        return HandleResult(result);
    }

    [HttpPatch("addresses/{addressId:guid}/default")]
    public async Task<IActionResult> SetDefaultAddress(Guid addressId)
    {
        // Dùng HttpPatch vì đây là hành động cập nhật 1 phần nhỏ (cờ IsDefault)
        var result = await Mediator.Send(new SetDefaultAddressCommand(addressId));
        return HandleResult(result);
    }
}