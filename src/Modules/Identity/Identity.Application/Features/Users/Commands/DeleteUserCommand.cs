using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

public record DeleteUserCommand(Guid TargetUserId) : IRequest<Result<string>>;

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty().WithMessage("ID người dùng không được để trống.");
    }
}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;
    private readonly ICurrentUser _currentUser;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == request.TargetUserId)
        {
            return Result<string>.Failure("Bạn không thể tự xóa tài khoản của chính mình.");
        }

        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            return Result<string>.Failure("Không tìm thấy người dùng hoặc tài khoản đã bị xóa từ trước.");
        }

        // CHỐNG KHÓA HỆ THỐNG: không xóa nếu đây là người dùng cuối cùng giữ 1 Role hệ thống.
        var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var violation = await ProtectedRoleGuard.CheckLastHolderViolationAsync(
            _roleRepository, _userRepository, user.Id, currentRoleIds, cancellationToken);
        if (violation != null)
        {
            return Result<string>.Failure(violation);
        }

        try
        {
            user.DeleteAccount();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            return Result<string>.Success("Đã xóa tài khoản thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}