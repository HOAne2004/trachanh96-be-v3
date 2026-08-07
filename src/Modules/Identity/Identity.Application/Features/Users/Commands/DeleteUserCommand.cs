using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND
// ==========================================================
public record DeleteUserCommand(Guid TargetUserId) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty().WithMessage("ID người dùng không được để trống.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;
    private readonly ICurrentUser _currentUser;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Chống tự xóa chính mình
        if (_currentUser.UserId == request.TargetUserId)
        {
            return Result<string>.Failure("Bạn không thể tự xóa tài khoản của chính mình.");
        }

        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            return Result<string>.Failure("Không tìm thấy người dùng hoặc tài khoản đã bị xóa từ trước.");
        }

        try
        {
            // 1. Thực thi Soft Delete + Revoke Sessions + Update Security Stamp trong Domain
            user.DeleteAccount();

            // 2. Lưu Database
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. ĐẨY CACHE: Đá văng các Access Token đang sống trên máy bị xóa
            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            return Result<string>.Success("Đã xóa tài khoản thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}