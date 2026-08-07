using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Auth.Commands;

// 1. Command
public record RevokeDeviceSessionCommand(
    Guid SessionId
) : IRequest<Result<bool>>;

// 2. Validator
public class RevokeDeviceSessionCommandValidator : AbstractValidator<RevokeDeviceSessionCommand>
{
    public RevokeDeviceSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("SessionId không được để trống.");
    }
}

// 3. Handler
public class RevokeDeviceSessionCommandHandler : IRequestHandler<RevokeDeviceSessionCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RevokeDeviceSessionCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(RevokeDeviceSessionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<bool>.Failure("Bạn chưa đăng nhập.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure("Không tìm thấy người dùng.");

        try
        {
            // Gọi hành vi Domain thu hồi Session theo Id
            user.RevokeSessionById(request.SessionId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (DomainException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
    }
}