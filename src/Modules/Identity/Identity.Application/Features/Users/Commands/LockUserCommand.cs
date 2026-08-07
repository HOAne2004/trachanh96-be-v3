using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands { 

// ==========================================================
// COMMAND KHÓA TÀI KHOẢN
// ==========================================================
public record LockUserCommand(Guid TargetUserId, int LockoutDays) : IRequest<Result<string>>;

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
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;

    public LockUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
    }

    public async Task<Result<string>> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null) return Result<string>.Failure("Không tìm thấy người dùng.");

        try
        {
            var lockoutEndTime = DateTime.UtcNow.AddDays(request.LockoutDays);

            // 1. Gọi Domain: LockAccount bên trong đã tự gọi RevokeAllSessions()
            user.LockAccount(lockoutEndTime);

            // 2. Bắt buộc thay đổi Security Stamp để kill JWT
            user.UpdateSecurityStamp();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. Cập nhật Redis/Cache ngay lập tức
            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            return Result<string>.Success($"Đã khóa tài khoản đến ngày {lockoutEndTime:dd/MM/yyyy HH:mm}.");
        }
        catch (DomainException ex) { return Result<string>.Failure(ex.Message); }
    }
}

}