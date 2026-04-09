
using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Commands
{
    // ==========================================================
    // 1. THE COMMAND (Dữ liệu gửi lên từ Controller)
    // ==========================================================
    public record UpdateUserAddressCommand(
        Guid UserPublicId, // Lấy từ Token để xác minh quyền sở hữu
        int AddressId,     // Lấy từ tham số URL
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
    // 2. THE VALIDATOR (Kiểm tra đầu vào)
    // ==========================================================
    public class UpdateUserAddressCommandValidator : AbstractValidator<UpdateUserAddressCommand>
    {
        public UpdateUserAddressCommandValidator()
        {
            RuleFor(x => x.UserPublicId).NotEmpty();
            RuleFor(x => x.AddressId).GreaterThan(0).WithMessage("ID địa chỉ không hợp lệ.");

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
    public class UpdateUserAddressCommandHandler : IRequestHandler<UpdateUserAddressCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public UpdateUserAddressCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> Handle(UpdateUserAddressCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User kèm theo Address (cần Include ở Repository)
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
            {
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");
            }

            try
            {
                // 2. Gọi logic Domain. 
                // Nếu AddressId truyền lên là của người khác hoặc đã bị xóa mềm, hàm này sẽ ném lỗi InvalidOperationException
                // Nếu IsDefault = true, hàm này sẽ tự động đi dọn dẹp (xóa IsDefault) của địa chỉ cũ.
                user.UpdateAddress(
                    request.AddressId,
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

                // 4. Trả kết quả (TransactionBehavior sẽ lưu DB)
                return Result<string>.Success("Cập nhật địa chỉ giao hàng thành công.");
            }
            catch (Exception ex)
            {
                // Bắt lỗi nghiệp vụ từ Domain (VD: "Không tìm thấy địa chỉ hợp lệ")
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}
