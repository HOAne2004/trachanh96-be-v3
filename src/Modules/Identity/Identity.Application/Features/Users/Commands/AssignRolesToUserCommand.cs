using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND
// Truyền ID chuẩn và Danh sách RoleIds
// ==========================================================
public record AssignRolesToUserCommand(
    Guid TargetUserId,
    List<Guid> RoleIds
) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class AssignRolesToUserCommandValidator : AbstractValidator<AssignRolesToUserCommand>
{
    public AssignRolesToUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty().WithMessage("ID người dùng không hợp lệ.");
        RuleFor(x => x.RoleIds)
            .NotNull()
            .NotEmpty().WithMessage("Phải chọn ít nhất 1 quyền cho người dùng.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;

    public AssignRolesToUserCommandHandler(
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

    public async Task<Result<string>> Handle(AssignRolesToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null || user.IsDeleted)
            return Result<string>.Failure("Không tìm thấy người dùng.");

        // 1. Validate xem các Role ID gửi lên có tồn tại thật trong DB không
        var validRoles = await _roleRepository.GetRolesByIdsAsync(request.RoleIds, cancellationToken);
        if (validRoles.Count() != request.RoleIds.Distinct().Count())
            return Result<string>.Failure("Có một hoặc nhiều Quyền (Role) không hợp lệ.");

        try
        {
            // 2. Đồng bộ Quyền (Domain Behavior)
            user.SyncRoles(request.RoleIds);

            // 3. BẢO MẬT: Đổi quyền -> Bắt buộc Logout để nạp lại JWT mới!
            user.RevokeAllSessions();
            user.UpdateSecurityStamp();

            // 4. Lưu DB
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Đá văng các Token cũ khỏi bộ nhớ đệm
            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            return Result<string>.Success("Đã cập nhật quyền thành công. Các phiên làm việc của người dùng này đã bị ngắt để áp dụng quyền mới.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}