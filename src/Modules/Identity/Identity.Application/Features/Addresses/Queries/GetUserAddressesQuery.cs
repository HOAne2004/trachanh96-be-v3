using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Queries
{
    // ==========================================================
    // 1. THE QUERY (Yêu cầu lấy dữ liệu)
    // ==========================================================
    public record GetUserAddressesQuery(Guid UserPublicId) : IRequest<Result<List<AddressDto>>>;

    // ==========================================================
    // 2. THE HANDLER (Truy xuất và Map sang DTO)
    // ==========================================================
    public class GetUserAddressesQueryHandler : IRequestHandler<GetUserAddressesQuery, Result<List<AddressDto>>>
    {
        private readonly IUserRepository _userRepository;

        public GetUserAddressesQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<List<AddressDto>>> Handle(GetUserAddressesQuery request, CancellationToken cancellationToken)
        {
            // Lấy User kèm theo danh sách Addresses (Đã được filter xóa mềm ở tầng Infra)
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);

            if (user == null)
            {
                return Result<List<AddressDto>>.Failure("Không tìm thấy tài khoản người dùng.");
            }

            // Map từ Domain Entity (Address) sang DTO (AddressDto)
            var addressDtos = user.Addresses.Select(a => new AddressDto
            {
                Id = a.Id,
                RecipientName = a.RecipientName,
                Phone = a.RecipientPhone.Value, // Sửa lại thành Phone (hoặc PhoneNumber tùy vào class AddressDto của bạn)
                FullAddress = a.FullAddress,
                IsDefault = a.IsDefault
            })
            // Sắp xếp: Địa chỉ mặc định luôn lên đầu tiên, sau đó xếp theo ID giảm dần (mới tạo lên trước)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.Id)
            .ToList();

            return Result<List<AddressDto>>.Success(addressDtos);
        }
    }
}
