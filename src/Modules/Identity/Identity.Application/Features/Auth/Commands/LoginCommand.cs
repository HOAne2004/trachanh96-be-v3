using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands
{
    public record AuthResponse(string AccessToken, Guid UserId, string FullName, string Role, string? ThumbnailUrl);

    public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtProvider _jwtProvider;

        public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtProvider jwtProvider)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
        }

        public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                return Result<AuthResponse>.Failure("Email hoặc mật khẩu không chính xác.");
            }

            var isPasswordValid = _passwordHasher.Verify(user.PasswordHash, request.Password);
            if (!isPasswordValid)
            {
                return Result<AuthResponse>.Failure("Email hoặc mật khẩu không chính xác.");
            }

            var token = _jwtProvider.Generate(user);

            return Result<AuthResponse>.Success(new AuthResponse(
                AccessToken: token,
                UserId: user.PublicId,
                FullName: user.FullName,
                Role: user.Role.ToString(),
                ThumbnailUrl: user.ThumbnailUrl
            ));
        }
    }
}