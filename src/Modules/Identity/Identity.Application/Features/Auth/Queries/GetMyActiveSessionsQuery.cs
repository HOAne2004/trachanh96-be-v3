using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Queries;

public record GetMyActiveSessionsQuery() : IRequest<Result<List<UserSessionDto>>>;

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

        var sessions = await _userRepository.GetActiveSessionsByUserIdAsync(_currentUser.UserId, cancellationToken);
        var currentSessionId = _currentUser.SessionId;

        var sessionDtos = sessions
            .Select(s => new UserSessionDto(
                SessionId: s.Id,
                DeviceName: s.DeviceName,
                IpAddress: s.IpAddress,
                ExpiryDate: s.ExpiryDate,
                CreatedAt: s.CreatedAt,
                IsCurrentSession: currentSessionId.HasValue && s.Id == currentSessionId.Value
            ))
            .ToList();

        return Result<List<UserSessionDto>>.Success(sessionDtos);
    }
}