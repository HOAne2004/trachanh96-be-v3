using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// Đổi tên từ VerifyEmailCommand -> ConfirmChangeEmailCommand để tránh trùng tên với
// Identity.Application.Features.Auth.Commands.VerifyEmailCommand (xác thực email đăng ký ban đầu).
public record ConfirmChangeEmailCommand(string OtpToken) : IRequest<Result<string>>;

public class ConfirmChangeEmailCommandValidator : AbstractValidator<ConfirmChangeEmailCommand>
{
    public ConfirmChangeEmailCommandValidator()
    {
        RuleFor(x => x.OtpToken).NotEmpty().WithMessage("Mã xác thực không được để trống.");
    }
}

public class ConfirmChangeEmailCommandHandler : IRequestHandler<ConfirmChangeEmailCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ISecurityCacheService _securityCacheService;

    public ConfirmChangeEmailCommandHandler(
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

    public async Task<Result<string>> Handle(ConfirmChangeEmailCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Result<string>.Failure("Bạn chưa đăng nhập.");
        }

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");
        }

        if (string.IsNullOrWhiteSpace(user.PendingEmail))
        {
            return Result<string>.Failure("Không có yêu cầu đổi email nào đang chờ xác nhận.");
        }

        // Kiểm tra race-condition TRƯỚC khi trừ lượt thử OTP: nếu email đích vừa bị người khác
        // chiếm giữa lúc Request và Confirm, không nên phạt oan lượt thử của người dùng vì lý do
        // không liên quan đến việc nhập sai mã.
        var emailTakenByOthers = await _userRepository.IsEmailExistsAsync(user.PendingEmail, cancellationToken);
        if (emailTakenByOthers)
        {
            return Result<string>.Failure("Email này vừa được sử dụng bởi một tài khoản khác. Vui lòng yêu cầu đổi sang email khác.");
        }

        try
        {
            // BƯỚC 2/2: OTP đúng mới thực sự áp dụng Email mới - Domain tự Revoke session +
            // đổi SecurityStamp bên trong.
            user.ConfirmEmailChange(request.OtpToken);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            return Result<string>.Success("Xác nhận đổi email thành công! Vui lòng đăng nhập lại bằng email mới.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}