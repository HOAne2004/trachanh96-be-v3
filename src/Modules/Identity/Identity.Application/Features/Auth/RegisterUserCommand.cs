using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Shared.Application.Interfaces;
using MediatR;

namespace Identity.Application.Features.Auth
{
    public record RegisterUserCommand(string Email, string FullName, string Password) : IRequest<Guid>;
    
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


        public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
        {
            private readonly IUserRepository _userRepository;
            private readonly IPasswordHasher _passwordHasher;
            private readonly IEmailService _emailService;
            private readonly IIdentityUnitOfWork _unitOfWork;
            public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IEmailService emailService, IIdentityUnitOfWork unitOfWork)
            {
                _userRepository = userRepository;
                _passwordHasher = passwordHasher;
                _emailService = emailService;
                _unitOfWork = unitOfWork;
            }

            public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
            {
                // 1. Kiểm tra Business Rule: Email đã tồn tại chưa?
                var emailExists = await _userRepository.IsEmailExistsAsync(request.Email, cancellationToken);
                if (emailExists)
                {
                    throw new InvalidOperationException("Email này đã được đăng ký trong hệ thống.");
                }

                // 2. Băm (Hash) mật khẩu
                var hashedPassword = _passwordHasher.Hash(request.Password);

                // 3. Tạo Entity User mới (Tận dụng constructor của Domain)
                var newUser = new User(
                    email: request.Email,
                    fullName: request.FullName,
                    passwordHash: hashedPassword
                );

                // 4. Lưu xuống Database
                var verifyToken = newUser.GenerateEmailVerificationToken();

                await _userRepository.AddAsync(newUser, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _emailService.SendVerificationEmailAsync(newUser.Email.Value, newUser.FullName, verifyToken);

                // 5. Trả về PublicId cho Client (dùng để định danh an toàn thay vì lộ Id tự tăng)
                return newUser.PublicId;
            }
        }
    }
}
