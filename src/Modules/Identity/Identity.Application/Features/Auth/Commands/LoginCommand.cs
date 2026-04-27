using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.Enums;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands;

// 1. DTO Response cho Login
public record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry,
    Guid UserId,
    string Email,
    string FullName,
    string Role
);

// 2. Command
public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<LoginResponseDto>>;

// 3. Validator
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống");
    }
}

// 4. Handler
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
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

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm user theo email
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            return Result<LoginResponseDto>.Failure("Email hoặc mật khẩu không đúng");
        }

        // 2. Kiểm tra trạng thái tài khoản
        if (user.IsDeleted)
        {
            return Result<LoginResponseDto>.Failure("Tài khoản đã bị xóa. Vui lòng liên hệ hỗ trợ.");
        }

        if (user.Status == UserStatusEnum.Locked)
        {
            if (user.LockoutEnd > DateTime.UtcNow)
            {
                return Result<LoginResponseDto>.Failure($"Tài khoản đang bị khóa đến {user.LockoutEnd:dd/MM/yyyy HH:mm}");
            }
            else
            {
                // Hết thời gian khóa, tự động mở khóa
                user.UnlockAccount();
            }
        }

        // 3. Kiểm tra mật khẩu
        if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            // TODO: Tăng FailedLoginAttempts
            return Result<LoginResponseDto>.Failure("Email hoặc mật khẩu không đúng");
        }

        // 4. Tạo tokens
        var accessToken = _jwtProvider.GenerateAccessToken(user);
        var refreshToken = _jwtProvider.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtProvider.GetRefreshTokenExpiry();

        // 5. Lưu Refresh Token vào User entity
        user.UpdateRefreshToken(refreshToken, refreshTokenExpiry);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 6. Trả về kết quả
        return Result<LoginResponseDto>.Success(new LoginResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiry: refreshTokenExpiry,
            UserId: user.PublicId,
            Email: user.Email.Value,
            FullName: user.FullName,
            Role: user.Role.ToString()
        ));
    }
}