using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Queries
{
    // ==========================================================
    // 1. THE QUERY (Yêu cầu lấy chi tiết một địa chỉ)
    // ==========================================================
    public record GetAddressByIdQuery(Guid UserPublicId, int AddressId) : IRequest<Result<AddressDto>>;

    // ==========================================================
    // 2. THE HANDLER (Xử lý truy vấn)
    // ==========================================================
    public class GetAddressByIdQueryHandler : IRequestHandler<GetAddressByIdQuery, Result<AddressDto>>
    {
        private readonly IUserRepository _userRepository;

        public GetAddressByIdQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<AddressDto>> Handle(GetAddressByIdQuery request, CancellationToken cancellationToken)
        {
            // 1. Lấy User kèm danh sách địa chỉ
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
            {
                return Result<AddressDto>.Failure("Không tìm thấy tài khoản người dùng.");
            }

            // 2. Tìm địa chỉ cụ thể trong tập hợp của User
            // Vì ta dùng Hard Delete nên không cần check IsDeleted nữa
            var address = user.Addresses.FirstOrDefault(a => a.Id == request.AddressId);

            if (address == null)
            {
                return Result<AddressDto>.Failure("Địa chỉ không tồn tại hoặc bạn không có quyền truy cập.");
            }

            // 3. Map sang DTO
            var dto = new AddressDto{
                Id = address.Id,
                RecipientName = address.RecipientName,
                Phone = address.RecipientPhone.Value,
                FullAddress = address.FullAddress,
                AddressDetail = address.AddressDetail,
                Province = address.Province,
                District = address.District,
                Commune = address.Commune,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsDefault = address.IsDefault
            };

            return Result<AddressDto>.Success(dto);
        }
    }
}
