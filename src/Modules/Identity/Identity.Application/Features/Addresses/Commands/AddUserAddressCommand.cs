using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Identity.Application.Features.Addresses.Commands
{
    // ==========================================================
    // 1. THE COMMAND 
    // ==========================================================
    public record AddUserAddressCommand(
        Guid UserPublicId,
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
    // 2. THE VALIDATOR (Giữ nguyên hoàn toàn)
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

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Vĩ độ không hợp lệ.")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Kinh độ không hợp lệ.")
                .When(x => x.Longitude.HasValue);
        }
    }

    // ==========================================================
    // 3. THE HANDLER
    // ==========================================================
    public class AddUserAddressCommandHandler : IRequestHandler<AddUserAddressCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public AddUserAddressCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> Handle(AddUserAddressCommand request, CancellationToken cancellationToken) // ĐÃ SỬA
        {
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
            {
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng."); // ĐÃ SỬA
            }

            try
            {
                // 1. Uỷ quyền cho Domain xử lý logic (Check < 5 địa chỉ, xử lý Default...)
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

                // 2. BẮT BUỘC THÊM: Đưa user vào trạng thái Tracking để TransactionBehavior lưu DB
                await _userRepository.UpdateAsync(user, cancellationToken);

                return Result<string>.Success("Thêm địa chỉ giao hàng thành công.");
            }
            catch (DomainException ex)
            {
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}