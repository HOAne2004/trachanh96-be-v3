using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND
// ==========================================================
public record SetDefaultAddressCommand(Guid AddressId) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class SetDefaultAddressCommandValidator : AbstractValidator<SetDefaultAddressCommand>
{
    public SetDefaultAddressCommandValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("ID địa chỉ không được để trống.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class SetDefaultAddressCommandHandler : IRequestHandler<SetDefaultAddressCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser; // Lấy thông tin user đang đăng nhập

    public SetDefaultAddressCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(SetDefaultAddressCommand request, CancellationToken cancellationToken)
    {
        // 1. Chặn request không hợp lệ
        if (!_currentUser.IsAuthenticated)
            return Result<string>.Failure("Bạn chưa đăng nhập.");

        // 2. Lấy User kèm theo danh sách Address (Cần Ensure Repository có Include(u => u.Addresses))
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
            return Result<string>.Failure("Không tìm thấy thông tin tài khoản.");

        try
        {
            // 3. Ủy quyền cho Domain xử lý logic gỡ/cài mặc định
            user.SetDefaultAddress(request.AddressId);

            // 4. Lưu thay đổi
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Đã cập nhật địa chỉ mặc định thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}