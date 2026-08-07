using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Addresses.Commands;

public record DeleteAddressCommand(Guid AddressId) : IRequest<Result<string>>;

public class DeleteAddressCommandValidator : AbstractValidator<DeleteAddressCommand>
{
    public DeleteAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty().WithMessage("ID địa chỉ không hợp lệ.");
    }
}

public class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteAddressCommandHandler(IUserRepository userRepository, IIdentityUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated) return Result<string>.Failure("Bạn chưa đăng nhập.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted) return Result<string>.Failure("Không tìm thấy tài khoản.");

        try
        {
            user.RemoveAddress(request.AddressId);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Đã xóa địa chỉ thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}