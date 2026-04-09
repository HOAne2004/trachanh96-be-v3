using FluentValidation;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Commands
{
    // ==========================================================
    // 1. THE COMMAND (Dữ liệu gửi lên từ Controller)
    // ==========================================================
    public record DeleteUserAddressCommand(
        Guid UserPublicId, // Lấy từ Token để đảm bảo chỉ chủ tài khoản mới được xóa
        int AddressId      // Lấy từ URL (VD: DELETE /addresses/5)
    ) : IRequest<Result<string>>;

    // ==========================================================
    // 2. THE VALIDATOR (Kiểm tra ID)
    // ==========================================================
    public class DeleteUserAddressCommandValidator : AbstractValidator<DeleteUserAddressCommand>
    {
        public DeleteUserAddressCommandValidator()
        {
            RuleFor(x => x.UserPublicId).NotEmpty();
            RuleFor(x => x.AddressId)
                .GreaterThan(0).WithMessage("ID địa chỉ không hợp lệ.");
        }
    }

    // ==========================================================
    // 3. THE HANDLER (Thực thi xóa mềm)
    // ==========================================================
    public class DeleteUserAddressCommandHandler : IRequestHandler<DeleteUserAddressCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;

        public DeleteUserAddressCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> Handle(DeleteUserAddressCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User kèm theo Address (cần Include ở Repository)
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId, cancellationToken);
            if (user == null)
            {
                return Result<string>.Failure("Không tìm thấy tài khoản người dùng.");
            }

            try
            {
                // 2. Gọi logic Domain (Xóa mềm và Tự động đôn địa chỉ khác lên làm mặc định nếu cần)
                user.RemoveAddress(request.AddressId);

                // 3. Cập nhật lại Entity
                await _userRepository.UpdateAsync(user, cancellationToken);

                // 4. Trả kết quả (TransactionBehavior sẽ tự động SaveChanges)
                return Result<string>.Success("Đã xóa địa chỉ thành công.");
            }
            catch (Exception ex)
            {
                // Bắt lỗi từ Domain (VD: "Không tìm thấy địa chỉ hợp lệ" nếu user truyền bậy ID của người khác)
                return Result<string>.Failure(ex.Message);
            }
        }
    }
}
