using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Users.Commands;

// ==========================================================
// 1. THE COMMAND
// ==========================================================
public record RestoreUserCommand(Guid TargetUserId) : IRequest<Result<string>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class RestoreUserCommandValidator : AbstractValidator<RestoreUserCommand>
{
    public RestoreUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty().WithMessage("ID người dùng không được để trống.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class RestoreUserCommandHandler : IRequestHandler<RestoreUserCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public RestoreUserCommandHandler(IUserRepository userRepository, IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        // Dùng hàm GetByIdWithDeletedAsync để tìm được cả bản ghi IsDeleted = true
        var user = await _userRepository.GetByIdWithDeletedAsync(request.TargetUserId, cancellationToken);

        if (user == null)
        {
            return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");
        }

        if (!user.IsDeleted)
        {
            return Result<string>.Failure("Tài khoản này hiện không ở trạng thái bị xóa.");
        }

        try
        {
            // Gọi hành vi RestoreAccount() đã viết sẵn trong User.cs
            user.RestoreAccount();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Khôi phục tài khoản thành công. Người dùng đã có thể đăng nhập lại.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}