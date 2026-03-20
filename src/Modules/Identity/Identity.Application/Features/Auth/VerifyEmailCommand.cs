using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Features.Auth;

public record VerifyEmailCommand(string Email, string Token) : IRequest<string>;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty().WithMessage("Mã xác thực không được để trống.");
    }
}

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public VerifyEmailCommandHandler(IUserRepository userRepository, IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Tài khoản không tồn tại.");

        user.VerifyEmail(request.Token);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return "Xác thực Email thành công! Bạn đã có thể đăng nhập.";
    }
}