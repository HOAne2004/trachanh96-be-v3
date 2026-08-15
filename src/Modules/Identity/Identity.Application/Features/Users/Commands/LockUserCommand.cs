using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

public record LockUserCommand(Guid TargetUserId, int LockoutDays, string? Reason) : IRequest<Result<string>>;

public class LockUserCommandValidator : AbstractValidator<LockUserCommand>
{
    public LockUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.LockoutDays).GreaterThan(0).WithMessage("Số ngày khóa phải lớn hơn 0.");
    }
}

public class LockUserCommandHandler : IRequestHandler<LockUserCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;

    public LockUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
    }

    public async Task<Result<string>> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null || user.IsDeleted)
            return Result<string>.Failure("Không tìm thấy người dùng hoặc tài khoản đã bị xóa.");

        var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var violation = await ProtectedRoleGuard.CheckLastHolderViolationAsync(
            _roleRepository, _userRepository, user.Id, currentRoleIds, cancellationToken);
        if (violation != null)
        {
            return Result<string>.Failure(violation);
        }

        try
        {
            var lockoutEndTime = DateTime.UtcNow.AddDays(request.LockoutDays);
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Vi phạm chính sách sử dụng dịch vụ." : request.Reason;

            // LockAccount (Domain) đã tự gọi RevokeAllSessions() + UpdateSecurityStamp() bên trong
            // (đã bổ sung ở lượt review trước) - KHÔNG gọi UpdateSecurityStamp() lần nữa ở đây.
            user.LockAccount(lockoutEndTime, reason);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            return Result<string>.Success($"Đã khóa tài khoản đến ngày {lockoutEndTime:dd/MM/yyyy HH:mm}.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}