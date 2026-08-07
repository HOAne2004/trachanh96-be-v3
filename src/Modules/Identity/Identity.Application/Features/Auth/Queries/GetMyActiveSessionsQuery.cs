using Identity.Application.DTOs;
using Identity.Application.DTOs.Request;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Queries;

// 1. Query
public record GetMyActiveSessionsQuery() : IRequest<Result<List<UserSessionDto>>>;

// 2. Handler
public class GetMyActiveSessionsQueryHandler : IRequestHandler<GetMyActiveSessionsQuery, Result<List<UserSessionDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetMyActiveSessionsQueryHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<UserSessionDto>>> Handle(GetMyActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<List<UserSessionDto>>.Failure("Bạn chưa đăng nhập.");

        // Lấy User kèm danh sách Sessions từ DB
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null)
            return Result<List<UserSessionDto>>.Failure("Không tìm thấy thông tin người dùng.");

        // Lọc các Session còn hiệu lực
        var activeSessions = user.Sessions
            .Where(s => s.IsValid())
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new UserSessionDto(
                SessionId: s.Id,
                DeviceName: s.DeviceName,
                IpAddress: s.IpAddress,
                ExpiryDate: s.ExpiryDate,
                CreatedAt: s.CreatedAt,
                IsCurrentSession: false // Frontend có thể tự so sánh hoặc truyền token/session ID hiện tại
            ))
            .ToList();

        return Result<List<UserSessionDto>>.Success(activeSessions);
    }
}