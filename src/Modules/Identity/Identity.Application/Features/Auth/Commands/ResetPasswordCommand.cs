using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand<Result<string>>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty().WithMessage("Mã xác thực không được để trống.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).WithMessage("Mật khẩu phải từ 6 ký tự.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    public ResetPasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return Result<string>.Failure("Email hoặc mã xác thực không đúng.");
        // Băm mật khẩu mới
        var newHashedPassword = _passwordHasher.Hash(request.NewPassword);

        // Gọi hàm Domain để xác nhận Token và đổi mật khẩu
        user.ConsumePasswordResetToken(request.Token, newHashedPassword);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<string>.Success("Đổi mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.");
    }
}