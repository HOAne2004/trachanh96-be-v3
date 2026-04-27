using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Auth.Commands;

// 1. Command
public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<Result<LoginResponseDto>>;

// 2. Validator
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token không được để trống");
    }
}

// 3. Handler
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtProvider _jwtProvider;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<LoginResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm user theo Refresh Token
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user == null)
        {
            return Result<LoginResponseDto>.Failure("Refresh token không hợp lệ");
        }

        // 2. Kiểm tra Refresh Token có hợp lệ không
        if (!user.IsRefreshTokenValid(request.RefreshToken))
        {
            return Result<LoginResponseDto>.Failure("Refresh token đã hết hạn hoặc không hợp lệ");
        }

        // 3. Tạo tokens mới
        var newAccessToken = _jwtProvider.GenerateAccessToken(user);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();
        var newRefreshTokenExpiry = _jwtProvider.GetRefreshTokenExpiry();

        // 4. Cập nhật Refresh Token mới (xoay vòng)
        user.UpdateRefreshToken(newRefreshToken, newRefreshTokenExpiry);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 5. Trả về kết quả
        return Result<LoginResponseDto>.Success(new LoginResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiry: newRefreshTokenExpiry,
            UserId: user.PublicId,
            Email: user.Email.Value,
            FullName: user.FullName,
            Role: user.Role.ToString()
        ));
    }
}