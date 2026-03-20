using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Features.Users;

// ==========================================================
// 1. THE RESPONSE DTO (Thông tin trả về)
// ==========================================================
public record UserProfileResponse(Guid PublicId, string Email, string FullName, string Role, string? Phone);

// ==========================================================
// 2. THE QUERY (Yêu cầu lấy dữ liệu)
// ==========================================================
public record GetProfileQuery(Guid PublicId) : IRequest<UserProfileResponse>;

// ==========================================================
// 3. THE HANDLER (Xử lý nghiệp vụ)
// ==========================================================
public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserProfileResponse>
{
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin User từ DB
        var user = await _userRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Tài khoản không tồn tại hoặc đã bị khóa.");
        }

        // 2. Map sang DTO và trả về (Tạm thời map tay, sau này dự án to ra ta sẽ xài AutoMapper)
        return new UserProfileResponse(
            PublicId: user.PublicId,
            Email: user.Email.Value,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            Phone: user.Phone?.Value // Vì Phone là Value Object nên lấy .Value
        );
    }
}