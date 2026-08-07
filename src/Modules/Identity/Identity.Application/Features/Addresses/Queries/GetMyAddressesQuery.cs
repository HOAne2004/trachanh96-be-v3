using Identity.Application.DTOs.Request;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Queries;


// ==========================================================
// 1. THE QUERY (Self-service: Không tham số)
// ==========================================================
public record GetMyAddressesQuery() : IRequest<Result<List<AddressDto>>>;

// ==========================================================
// 2. THE HANDLER
// ==========================================================
public class GetMyAddressesQueryHandler : IRequestHandler<GetMyAddressesQuery, Result<List<AddressDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser; // Chìa khóa bảo mật

    public GetMyAddressesQueryHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<AddressDto>>> Handle(GetMyAddressesQuery request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra đăng nhập
        if (!_currentUser.IsAuthenticated)
        {
            return Result<List<AddressDto>>.Failure("Bạn chưa đăng nhập.");
        }

        // 2. Lấy User từ DB (Repository đã đảm bảo Include Addresses)
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            return Result<List<AddressDto>>.Failure("Không tìm thấy thông tin tài khoản.");
        }

        // 3. Map từ Domain Entity sang DTO
        var addressDtos = user.Addresses.Select(a => new AddressDto(
            Id: a.Id,
            RecipientName: a.RecipientName,
            Phone: a.RecipientPhone.Value, // Lấy chuỗi từ ValueObject PhoneNumber
            FullAddress: a.FullAddress,
            AddressDetail: a.AddressDetail,
            Province: a.Province,
            District: a.District,
            Commune: a.Commune,
            Latitude: a.Location?.Latitude,   
            Longitude: a.Location?.Longitude,
            IsDefault: a.IsDefault
        ))
        // Sắp xếp: Mặc định lên đầu, sau đó ưu tiên địa chỉ mới tạo
        .OrderByDescending(a => a.IsDefault)
        .ThenByDescending(a => a.Id)
        .ToList();

        return Result<List<AddressDto>>.Success(addressDtos);
    }
}