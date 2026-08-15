using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Queries;

public record GetMyAddressesQuery() : IRequest<Result<List<AddressDto>>>;

public class GetMyAddressesQueryHandler : IRequestHandler<GetMyAddressesQuery, Result<List<AddressDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetMyAddressesQueryHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<AddressDto>>> Handle(GetMyAddressesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Result<List<AddressDto>>.Failure("Bạn chưa đăng nhập.");
        }

        var addresses = await _userRepository.GetAddressesByUserIdAsync(_currentUser.UserId, cancellationToken);

        var addressDtos = addresses.Select(a => new AddressDto(
            Id: a.Id,
            RecipientName: a.RecipientName,
            Phone: a.RecipientPhone.Value,
            FullAddress: a.FullAddress,
            AddressDetail: a.AddressDetail,
            Province: a.Province,
            District: a.District,
            Commune: a.Commune,
            Latitude: a.Location?.Latitude,
            Longitude: a.Location?.Longitude,
            IsDefault: a.IsDefault
        ))
        // Sắp xếp theo CreatedAt tường minh thay vì dựa vào Id - dù Address.Id dùng Guid v7
        // (có tính sắp thứ tự theo thời gian) nên ThenByDescending(a => a.Id) trước đây vẫn
        // cho kết quả đúng, việc dùng CreatedAt rõ ràng hơn cho người đọc code sau này,
        // không cần biết đặc tính ngầm của Guid v7 mới hiểu được ý đồ sắp xếp.
        .OrderByDescending(a => a.IsDefault)
        .ToList();

        return Result<List<AddressDto>>.Success(addressDtos);
    }
}