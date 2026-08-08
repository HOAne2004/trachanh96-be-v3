using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

public record UpdateUserByAdminCommand(
    Guid TargetUserId,
    string FullName,
    string Email,
    string? PhoneNumber
) : IRequest<Result<string>>;

public class UpdateUserByAdminCommandValidator : AbstractValidator<UpdateUserByAdminCommand>
{
    public UpdateUserByAdminCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^0[35789][0-9]{8}$").WithMessage("Số điện thoại không đúng định dạng VN.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}

public class UpdateUserByAdminCommandHandler : IRequestHandler<UpdateUserByAdminCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISecurityCacheService _securityCacheService;

    public UpdateUserByAdminCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ISecurityCacheService securityCacheService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _securityCacheService = securityCacheService;
    }

    public async Task<Result<string>> Handle(UpdateUserByAdminCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null || user.IsDeleted)
            return Result<string>.Failure("Không tìm thấy tài khoản người dùng hoặc tài khoản đã bị xóa.");

        var emailChanged = !user.Email.Value.Equals(request.Email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var isEmailExists = await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken);
            if (isEmailExists)
                return Result<string>.Failure("Email này đã được sử dụng bởi một tài khoản khác.");
        }

        try
        {
            // AdminUpdateUser (Domain) tự RevokeAllSessions() + UpdateSecurityStamp() nếu email đổi
            user.AdminUpdateUser(request.FullName, request.Email, request.PhoneNumber);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (emailChanged)
            {
                // Write-through ngay, không chờ UserSecurityCacheInvalidationHandler qua Outbox (~10s trễ)
                // - nhất quán với LockUserCommand/ChangePasswordCommand/AssignRolesToUserCommand.
                await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));
                return Result<string>.Success("Cập nhật thông tin thành công. Email đã thay đổi nên các phiên đăng nhập cũ đã bị ngắt.");
            }

            return Result<string>.Success("Cập nhật thông tin người dùng thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}