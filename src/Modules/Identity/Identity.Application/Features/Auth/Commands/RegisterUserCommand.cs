using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Shared.Application.Models;
using Shared.Application.Interfaces;

namespace Identity.Application.Features.Auth.Commands
{
    public record RegisterUserCommand(string Email, string FullName, string Password) : ICommand<Result<Guid>>;

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
                .MinimumLength(6).WithMessage("Mật khẩu phải dài ít nhất 6 ký tự.");
        }
    }

    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IEmailService emailService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var emailExists = await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken);
            if (emailExists)
            {
                // ĐÃ SỬA: Dùng Result thay vì Throw Exception
                return Result<Guid>.Failure("Email này đã được đăng ký trong hệ thống.");
            }

            var hashedPassword = _passwordHasher.Hash(request.Password);

            var newUser = new User(
                email: request.Email,
                fullName: request.FullName,
                passwordHash: hashedPassword
            );

            var verifyToken = newUser.GenerateEmailVerificationToken();

            _userRepository.Add(newUser);
            await _emailService.SendVerificationEmailAsync(newUser.Email.Value, newUser.FullName, verifyToken);

            return Result<Guid>.Success(newUser.PublicId);
        }
    }
}