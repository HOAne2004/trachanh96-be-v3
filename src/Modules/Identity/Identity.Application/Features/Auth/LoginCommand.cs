using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Features.Auth
{
    // ==========================================================
    // 1. THE RESPONSE DTO (Dữ liệu trả về cho Frontend)
    // ==========================================================
    public record AuthResponse(string AccessToken, Guid UserId, string FullName, string Role);

    // ==========================================================
    // 2. THE COMMAND (Dữ liệu gửi lên từ Frontend)
    // ==========================================================
    public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

    // ==========================================================
    // 3. THE VALIDATOR (Kiểm tra đầu vào)
    // ==========================================================
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không đúng định dạng.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.");
        }
    }

    // ==========================================================
    // 4. THE HANDLER (Xử lý nghiệp vụ Đăng nhập)
    // ==========================================================
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtProvider _jwtProvider;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtProvider jwtProvider)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
        }

        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User theo Email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
            }

            // 2. Kiểm tra mật khẩu
            var isPasswordValid = _passwordHasher.Verify(user.PasswordHash, request.Password);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
            }

            // 3. Nếu đúng hết -> Tạo JWT Token
            var token = _jwtProvider.Generate(user);

            // 4. Trả kết quả về
            return new AuthResponse(
                AccessToken: token,
                UserId: user.PublicId,
                FullName: user.FullName,
                Role: user.Role.ToString()
            );
        }
    }
}
