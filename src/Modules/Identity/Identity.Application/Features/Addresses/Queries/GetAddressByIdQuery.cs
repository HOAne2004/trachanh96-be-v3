using FluentValidation;
using Identity.Application.DTOs.Request;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Queries;

// ==========================================================
// 1. THE QUERY (Chỉ nhận AddressId, cấm nhận UserId từ ngoài)
// ==========================================================
public record GetAddressByIdQuery(Guid AddressId) : IRequest<Result<AddressDto>>;

// ==========================================================
// 2. THE VALIDATOR
// ==========================================================
public class GetAddressByIdQueryValidator : AbstractValidator<GetAddressByIdQuery>
{
    public GetAddressByIdQueryValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("ID địa chỉ không được để trống.");
    }
}

// ==========================================================
// 3. THE HANDLER
// ==========================================================
public class GetAddressByIdQueryHandler : IRequestHandler<GetAddressByIdQuery, Result<AddressDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser; // Chìa khóa chống IDOR

    public GetAddressByIdQueryHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<AddressDto>> Handle(GetAddressByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Chặn request không hợp lệ
        if (!_currentUser.IsAuthenticated)
            return Result<AddressDto>.Failure("Bạn chưa đăng nhập.");

        // 2. Lấy User từ DB qua _currentUser.UserId (Đã Include Addresses)
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            return Result<AddressDto>.Failure("Không tìm thấy tài khoản người dùng.");
        }

        // 3. Tìm địa chỉ cụ thể trong tập hợp địa chỉ của CHÍNH User đó
        var address = user.Addresses.FirstOrDefault(a => a.Id == request.AddressId);

        if (address == null)
        {
            return Result<AddressDto>.Failure("Địa chỉ không tồn tại hoặc bạn không có quyền truy cập.");
        }

        // 4. Map sang DTO an toàn với Value Object
        var dto = new AddressDto(
            Id: address.Id,
            RecipientName: address.RecipientName,
            Phone: address.RecipientPhone.Value, // Lấy giá trị chuỗi từ ValueObject
            FullAddress: address.FullAddress,
            AddressDetail: address.AddressDetail,
            Province: address.Province,
            District: address.District,
            Commune: address.Commune,
            Latitude: address.Location?.Latitude,   // Bắt null an toàn
            Longitude: address.Location?.Longitude, // Bắt null an toàn
            IsDefault: address.IsDefault
        );

        return Result<AddressDto>.Success(dto);
    }
}