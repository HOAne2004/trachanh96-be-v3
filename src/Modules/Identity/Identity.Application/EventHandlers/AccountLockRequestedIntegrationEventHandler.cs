using Identity.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;
using Shared.Domain.Exceptions;
using Shared.Domain.IntegrationEvents;

namespace Identity.Application.EventHandlers;

/// <summary>
/// Điểm tiếp nhận DUY NHẤT cho mọi yêu cầu khóa tài khoản đến từ Module khác. Tái sử dụng
/// nguyên vẹn User.LockAccount() đã có - không viết lại logic khóa, chỉ đóng vai trò cầu nối.
/// </summary>
public class AccountLockRequestedIntegrationEventHandler : INotificationHandler<AccountLockRequestedIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;
    private readonly ILogger<AccountLockRequestedIntegrationEventHandler> _logger;

    public AccountLockRequestedIntegrationEventHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService,
        ILogger<AccountLockRequestedIntegrationEventHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
        _logger = logger;
    }

    public async Task Handle(AccountLockRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning(
                "Nhận yêu cầu khóa tài khoản {UserId} từ module '{Source}' nhưng không tìm thấy User.",
                notification.UserId, notification.SourceModule);
            return;
        }

        try
        {
            var lockoutEnd = DateTime.UtcNow.AddDays(notification.LockDurationDays);

            // LockAccount (Domain) tự RevokeAllSessions() + UpdateSecurityStamp() + raise
            // UserAccountLockedEvent - chuỗi xử lý cache invalidation đã có từ trước tự động
            // áp dụng, không cần lặp lại ở đây.
            var reason = string.IsNullOrWhiteSpace(notification.Reason) ? "Vi phạm chính sách sử dụng dịch vụ." : notification.Reason;
            user.LockAccount(lockoutEnd, reason);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            _logger.LogInformation(
                "Đã khóa tài khoản {UserId} theo yêu cầu từ module '{Source}'. Lý do: {Reason}. Hạn khóa: {LockoutEnd}",
                notification.UserId, notification.SourceModule, reason, lockoutEnd);
        }
        catch (DomainException ex)
        {
            // VD: tài khoản đã bị khóa từ trước - không phải lỗi nghiêm trọng, chỉ ghi log.
            _logger.LogWarning(
                "Không thể khóa tài khoản {UserId} theo yêu cầu từ '{Source}': {Message}",
                notification.UserId, notification.SourceModule, ex.Message);
        }
    }
}