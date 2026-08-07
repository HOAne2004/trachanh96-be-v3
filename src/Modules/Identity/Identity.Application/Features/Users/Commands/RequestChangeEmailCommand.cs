using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND
// Đã loại bỏ UserPublicId. Self-service 100%.
// ==========================================================
public record RequestChangeEmailCommand(
    string NewEmail
) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class RequestChangeEmailCommandValidator : AbstractValidator<RequestChangeEmailCommand>
{
    public RequestChangeEmailCommandValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("Email mới không được để trống.")
            .EmailAddress().WithMessage("Email mới không hợp lệ.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class RequestChangeEmailCommandHandler : IRequestHandler<RequestChangeEmailCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ISecurityCacheService _securityCacheService;

    public RequestChangeEmailCommandHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IIdentityUnitOfWork unitOfWork,
        IEmailService emailService,
        ISecurityCacheService securityCacheService)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _securityCacheService = securityCacheService;
    }

    public async Task<Result<string>> Handle(RequestChangeEmailCommand request, CancellationToken cancellationToken)
    {
        // 1. Cổng gác bảo mật
        if (!_currentUser.IsAuthenticated)
            return Result<string>.Failure("Bạn chưa đăng nhập.");

        // 2. Tránh việc đổi sang email giống hệt email cũ hoặc bị trùng với người khác
        var emailExists = await _userRepository.IsEmailExistsAsync(request.NewEmail, cancellationToken);
        if (emailExists)
            return Result<string>.Failure("Email này đã được sử dụng bởi một tài khoản khác.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
            return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");

        try
        {
            // 3. Gọi Domain Behavior: Đổi Email và reset trạng thái EmailVerified = false
            user.ChangeEmail(request.NewEmail);

            // 4. Sinh OTP (Đẩy logic sinh mã về Application Layer)
            var otpToken = Guid.NewGuid().ToString("N")[..6].ToUpper(); // Sinh mã 6 ký tự
            user.SetVerificationToken(otpToken, expiryHours: 24);

            // 5. BẢO MẬT TỐI CAO: Vì Email (Tên đăng nhập) đã thay đổi, 
            // các Token cũ phải chết ngay lập tức!
            user.UpdateSecurityStamp();
            user.RevokeAllSessions();

            // 6. Lưu xuống DB qua Unit of Work
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 7. Cập nhật Cache để khóa ngay JWT cũ trên mọi thiết bị
            await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

            // 8. Gửi Email (Có try-catch độc lập)
            try
            {
                await _emailService.SendChangeEmailOtpAsync(request.NewEmail, user.FullName, otpToken);
            }
            catch (Exception)
            {
                // Tùy chiến lược công ty, có thể cho phép người dùng ấn "Gửi lại OTP" sau.
            }

            return Result<string>.Success("Đã cập nhật email. Phiên đăng nhập hiện tại đã kết thúc, vui lòng đăng nhập lại và kiểm tra hộp thư để xác thực email mới.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}