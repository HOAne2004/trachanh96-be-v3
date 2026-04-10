using Identity.Application.Features.Addresses.Commands;
using Identity.Application.Features.Addresses.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Models;
using Shared.Presentation.Controllers;
using System.Security.Claims;

namespace Identity.Presentation.Controllers;

[Route("api/identity/users/me/addresses")]
[Authorize]
public class UserAddressesController : BaseApiController
{
    private Guid GetCurrentUserPublicId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var publicId) ? publicId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAddresses()
    {
        var result = await Mediator.Send(new GetUserAddressesQuery(GetCurrentUserPublicId()));
        return HandleResult(result);
    }

    [HttpGet("{addressId:int}")]
    public async Task<IActionResult> GetAddressById(int addressId)
    {
        var result = await Mediator.Send(new GetAddressByIdQuery(GetCurrentUserPublicId(), addressId));
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddAddress([FromBody] AddUserAddressCommand command)
    {
        var secureCommand = command with { UserPublicId = GetCurrentUserPublicId() };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Thêm địa chỉ giao hàng thành công!");
    }

    [HttpPut("{addressId:int}")]
    public async Task<IActionResult> UpdateAddress(int addressId, [FromBody] UpdateUserAddressCommand command)
    {
        if (addressId != command.AddressId)
            return BadRequest(new ErrorResponse("INVALID_ID", "ID không khớp."));

        var secureCommand = command with { UserPublicId = GetCurrentUserPublicId() };
        var result = await Mediator.Send(secureCommand);
        return HandleResult(result, "Cập nhật địa chỉ thành công!");
    }

    [HttpDelete("{addressId:int}")]
    public async Task<IActionResult> DeleteAddress(int addressId)
    {
        var result = await Mediator.Send(new DeleteUserAddressCommand(GetCurrentUserPublicId(), addressId));
        return HandleResult(result, "Xóa địa chỉ thành công!");
    }
}