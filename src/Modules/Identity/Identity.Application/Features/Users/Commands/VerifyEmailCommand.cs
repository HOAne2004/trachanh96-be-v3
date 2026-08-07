using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND (Self-Service: Bỏ UserPublicId)
// ==========================================================
public record VerifyEmailCommand(string OtpToken) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.OtpToken).NotEmpty().WithMessage("Mã xác thực không được để trống.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // 1. Chặn request nếu chưa có Token đăng nhập
        if (!_currentUser.IsAuthenticated)
        {
            return Result<string>.Failure("Bạn chưa đăng nhập.");
        }

        // 2. Lấy User an toàn từ JWT Context
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");
        }

        try
        {
            // 3. Ủy quyền cho Domain Behavior kiểm tra OTP
            user.VerifyEmail(request.OtpToken);

            // 4. Cập nhật và lưu vào DB
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Xác thực email thành công!");
        }
        catch (DomainException ex)
        {
            // Bắt chính xác lỗi nghiệp vụ từ Domain (Mã sai, hết hạn...)
            return Result<string>.Failure(ex.Message);
        }
    }
}