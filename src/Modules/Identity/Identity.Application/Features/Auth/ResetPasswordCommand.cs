using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Features.Auth;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<string>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty().WithMessage("Mã xác thực không được để trống.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).WithMessage("Mật khẩu phải từ 6 ký tự.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdentityUnitOfWork _unitOfWork;
    public ResetPasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("Email hoặc mã xác thực không đúng.");

        // Băm mật khẩu mới
        var newHashedPassword = _passwordHasher.Hash(request.NewPassword);

        // Gọi hàm Domain để xác nhận Token và đổi mật khẩu
        user.ConsumePasswordResetToken(request.Token, newHashedPassword);

        // Lưu vào DB
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return "Đổi mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.";
    }
}