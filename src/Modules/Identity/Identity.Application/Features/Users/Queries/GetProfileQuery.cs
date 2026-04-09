using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Users.Queries;

public record UserProfileResponse(Guid PublicId, string Email, string FullName, string Role, string? Phone);

public record GetProfileQuery(Guid PublicId) : IRequest<Result<UserProfileResponse>>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<UserProfileResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);

        if (user == null)
        {
            return Result<UserProfileResponse>.Failure("Tài khoản không tồn tại hoặc đã bị khóa.");
        }

        var responseDto = new UserProfileResponse(
            PublicId: user.PublicId,
            Email: user.Email.Value,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            Phone: user.Phone?.Value
        );

        return Result<UserProfileResponse>.Success(responseDto);
    }
}