using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Addresses.Commands;

public record UpdateAddressCommand(
    Guid AddressId,
    string RecipientName,
    string PhoneNumber,
    string AddressDetail,
    string Province,
    string District,
    string Commune,
    double? Latitude,
    double? Longitude,
    bool IsDefault
) : IRequest<Result<string>>;

public class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$");
        RuleFor(x => x.AddressDetail).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Province).NotEmpty();
        RuleFor(x => x.District).NotEmpty();
        RuleFor(x => x.Commune).NotEmpty();
    }
}

public class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateAddressCommandHandler(IUserRepository userRepository, IIdentityUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated) return Result<string>.Failure("Bạn chưa đăng nhập.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted) return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");

        try
        {
            user.UpdateAddress(
                request.AddressId, request.RecipientName, request.PhoneNumber,
                request.AddressDetail, request.Province, request.District,
                request.Commune, request.Latitude, request.Longitude, request.IsDefault
            );

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success("Cập nhật địa chỉ thành công.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}