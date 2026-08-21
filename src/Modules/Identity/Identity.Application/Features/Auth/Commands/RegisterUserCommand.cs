using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands;

public record RegisterUserCommand(
    string Email,
    string FullName,
    string Password
) : IRequest<Result<Guid>>;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải dài ít nhất 6 ký tự.")
            .Matches(@"[A-Z]+").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa.")
            .Matches(@"[0-9]+").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.");
    }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            return Result<Guid>.Failure("Email này đã được đăng ký trong hệ thống.");
        }

        var hashedPassword = _passwordHasher.Hash(request.Password);

        var newUser = new User(
            email: request.Email,
            fullName: request.FullName,
            passwordHash: hashedPassword
        );

        var customerRole = await _roleRepository.GetByNormalizedNameAsync("CUSTOMER", cancellationToken);
        if (customerRole != null)
        {
            newUser.AssignRole(customerRole.Id);
        }

        var verifyToken = Guid.NewGuid().ToString("N");
        newUser.SetVerificationToken(verifyToken, expiryHours: 24);

        // KHÔNG gọi IEmailService trực tiếp nữa - raise event, để ProcessOutboxMessagesJob
        // (chạy nền, KHÔNG chặn HTTP response) xử lý gửi email. Đây là lý do trước đây
        // Register bị timeout: SMTP có thể treo hàng chục giây/vài phút, chặn đứng response
        // dù DB đã ghi thành công.
        newUser.AddDomainEvent(new UserRegisteredEvent(newUser.Id, newUser.Email.Value, newUser.FullName, verifyToken));

        _userRepository.Add(newUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(newUser.Id);
    }
}