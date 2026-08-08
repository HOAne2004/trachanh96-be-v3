using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND (Data Transfer Object)
// KHÔNG CHỨA USER ID! Chỉ chứa những gì được phép sửa.
// ==========================================================
public record UpdateProfileCommand(
    string FullName,
    string? PhoneNumber,
    string? ThumbnailUrl
) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^0[35789][0-9]{8}$").WithMessage("Số điện thoại không đúng định dạng VN.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        // 1. Cổng gác bảo mật (Đảm bảo Request đến từ một Token hợp lệ)
        if (!_currentUser.IsAuthenticated)
        {
            return Result<string>.Failure("Bạn chưa đăng nhập.");
        }

        // 2. Lấy User từ ID đáng tin cậy (Móc từ JWT, cấm truyền từ ngoài vào)
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            return Result<string>.Failure("Không tìm thấy tài khoản người dùng hoặc tài khoản đã bị khóa.");
        }

        try
        {
            // 3. Ủy quyền cho Domain Behavior xử lý (Rich Domain Model)
            user.UpdateProfile(request.FullName, request.PhoneNumber, request.ThumbnailUrl);

            // 4. Lưu trữ (Persistence)
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Commit Transaction tường minh
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Cập nhật thông tin cá nhân thành công.");
        }
        catch (DomainException ex)
        {
            // CHỈ bắt lỗi nghiệp vụ (ví dụ: Tên quá dài, Số điện thoại sai định dạng...)
            return Result<string>.Failure(ex.Message);
        }
        // Các Exception liên quan đến DB (như timeout) sẽ văng xuyên qua đây,
        // lên GlobalExceptionHandler để trả 500 Internal Server Error.
    }
}