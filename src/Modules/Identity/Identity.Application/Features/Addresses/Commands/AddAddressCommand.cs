using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Addresses.Commands;

// ==========================================================
// 1. THE COMMAND (Bỏ UserPublicId)
// ==========================================================
public record AddAddressCommand(
    string RecipientName,
    string PhoneNumber,
    string AddressDetail,
    string Province,
    string? District,
    string Commune,
    double? Latitude,
    double? Longitude,
    bool IsDefault
) : IRequest<Result<Guid>>; 

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    public AddAddressCommandValidator()
    {
        RuleFor(x => x.RecipientName)
            .NotEmpty()
            .WithMessage("Tên người nhận không được để trống.")
            .MaximumLength(150);

        RuleFor(x => x.PhoneNumber)
                    .Matches(@"^0[35789][0-9]{8}$").WithMessage("Số điện thoại không đúng định dạng VN.")
                    .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.AddressDetail)
            .NotEmpty()
            .WithMessage("Tỉnh/Thành phố không được để trống.")
            .MaximumLength(300);
        RuleFor(x => x.Commune)
            .NotEmpty()
            .WithMessage("Xã/Phường không được để trống.");
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Vĩ độ phải nằm trong khoảng từ -90 đến 90.")
            .When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Kinh độ phải nằm trong khoảng từ -180 đến 180.")
            .When(x => x.Longitude.HasValue);
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class AddAddressCommandHandler : IRequestHandler<AddAddressCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddAddressCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(AddAddressCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated) return Result<Guid>.Failure("Bạn chưa đăng nhập.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted) return Result<Guid>.Failure("Không tìm thấy tài khoản người dùng.");

        try
        {
            // Ủy quyền Domain tạo và lấy ra Entity Address mới
            var newAddress = user.AddAddress(
                request.RecipientName, request.PhoneNumber, request.AddressDetail,
                request.Province, request.District, request.Commune,
                request.Latitude, request.Longitude, request.IsDefault
            );

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(newAddress.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}