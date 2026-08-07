using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Auth.Commands;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : IRequest<Result<string>>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty().WithMessage("Mã xác thực không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải từ 6 ký tự.")
            .Matches(@"[A-Z]+").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa.")
            .Matches(@"[0-9]+").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityCacheService _securityCacheService;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ISecurityCacheService securityCacheService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _securityCacheService = securityCacheService;
    }

    public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return Result<string>.Failure("Email hoặc mã xác thực không đúng.");

        var newHashedPassword = _passwordHasher.Hash(request.NewPassword);

        try
        {
            // LƯU Ý: Hàm này bên trong đã tự động gọi UpdateSecurityStamp() và RevokeAllSessions()
            user.ResetPassword(request.Token, newHashedPassword);
        }
        catch (DomainException ex)
        {
            // Token sai/hết hạn là tình huống THƯỜNG GẶP (người dùng gõ nhầm mã), không phải
            // edge-case hiếm - bắt tại đây để giữ đúng shape Result<T>.Failure, tránh rơi xuống
            // GlobalExceptionHandler với response shape khác (ErrorResponse, HTTP 400).
            return Result<string>.Failure(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Ghi đè cache SecurityStamp NGAY với giá trị mới (write-through), nhanh hơn so với
        // xóa cache rồi chờ cold-start. Lưu ý: UserSecurityCacheInvalidationHandler (qua Outbox,
        // khi UserPasswordResetEvent được publish) sẽ gọi RemoveSecurityStampAsync sau đó tối đa
        // ~10s - ghi đè giá trị đúng vừa set ở đây, gây 1 lần cache-miss thừa ở lần xác thực JWT
        // tiếp theo (không sai, chỉ lãng phí 1 round-trip DB). Không ảnh hưởng tính đúng đắn.
        await _securityCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp.ToString(), TimeSpan.FromMinutes(15));

        return Result<string>.Success("Đổi mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.");
    }
}