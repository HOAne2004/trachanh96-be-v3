using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands;

// 1. Command
public record RevokeAllSessionsCommand() : IRequest<Result<bool>>;

// 2. Handler
public class RevokeAllSessionsCommandHandler : IRequestHandler<RevokeAllSessionsCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ISecurityCacheService _securityCacheService;

    public RevokeAllSessionsCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ISecurityCacheService securityCacheService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _securityCacheService = securityCacheService;
    }

    public async Task<Result<bool>> Handle(RevokeAllSessionsCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<bool>.Failure("Bạn chưa đăng nhập.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure("Không tìm thấy người dùng.");

        // 1. Thu hồi toàn bộ Sessions trong Database
        user.RevokeAllSessions();

        // 2. Đổi Security Stamp trong Domain
        user.UpdateSecurityStamp();

        // 3. Lưu CSDL
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. CHỐT CHẶN BẢO MẬT: Cập nhật SecurityStamp mới lên Redis/MemoryCache 
        // Giúp vô hiệu hóa NGAY LẬP TỨC mọi JWT Access Token đang còn hạn!
        await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

        return Result<bool>.Success(true);
    }
}