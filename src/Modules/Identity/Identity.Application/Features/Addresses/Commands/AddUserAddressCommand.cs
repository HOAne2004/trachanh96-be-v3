using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Commands
{
    // ==========================================================
    // 1. THE COMMAND (Dữ liệu gửi lên từ Controller)
    // ==========================================================
    public record AddUserAddressCommand(
        Guid UserPublicId, // Lấy từ Token ở Controller, KHÔNG phải do người dùng tự nhập từ Body
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

    // ==========================================================
    // 2. THE VALIDATOR (Kiểm tra đầu vào trước khi lọt vào Handler)
    // ==========================================================
    public class AddUserAddressCommandValidator : AbstractValidator<AddUserAddressCommand>
    {
        public AddUserAddressCommandValidator()
        {
            RuleFor(x => x.UserPublicId).NotEmpty();

            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("Tên người nhận không được để trống.")
                .MaximumLength(150).WithMessage("Tên người nhận không được vượt quá 150 ký tự.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$").WithMessage("Số điện thoại không đúng định dạng VN.");

            RuleFor(x => x.AddressDetail)
                .NotEmpty().WithMessage("Địa chỉ chi tiết không được để trống.")
                .MaximumLength(300).WithMessage("Địa chỉ chi tiết không được vượt quá 300 ký tự.");

            RuleFor(x => x.Province).NotEmpty().WithMessage("Tỉnh/Thành phố không được để trống.");
            RuleFor(x => x.District).NotEmpty().WithMessage("Quận/Huyện không được để trống.");
            RuleFor(x => x.Commune).NotEmpty().WithMessage("Phường/Xã không được để trống.");

            // Kiểm tra tọa độ GPS nếu có truyền lên
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Vĩ độ không hợp lệ.")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Kinh độ không hợp lệ.")
                .When(x => x.Longitude.HasValue);
        }
    }

    // ==========================================================
    // 3. THE HANDLER (Xử lý logic giao tiếp với Domain)
    // ==========================================================
    public class AddUserAddressCommandHandler : IRequestHandler<AddUserAddressCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public AddUserAddressCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> Handle(AddUserAddressCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy thông tin User kèm theo danh sách địa chỉ (cần dùng Include(_addresses) ở tầng Infrastructure)
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
            {
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");
            }

            try
            {
                // 2. Chuyển quyền xử lý nghiệp vụ lõi cho Domain (User.cs)
                // Lỗi vượt quá 5 địa chỉ sẽ được quăng ra từ trong hàm này.
                user.AddAddress(
                    request.RecipientName,
                    request.PhoneNumber,
                    request.AddressDetail,
                    request.Province,
                    request.District,
                    request.Commune,
                    request.Latitude,
                    request.Longitude,
                    request.IsDefault
                );

                // 3. Cập nhật lại Entity
                await _userRepository.UpdateAsync(user, cancellationToken);

                // 4. Trả về thông báo thành công (TransactionBehavior sẽ tự động bắt lấy và SaveChanges)
                return Result<string>.Success("Thêm địa chỉ giao hàng thành công.");
            }
            catch (Exception ex)
            {
                // Bắt trọn các lỗi nghiệp vụ như "Không thể thêm quá 5 địa chỉ" từ DomainException/InvalidOperationException
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}
