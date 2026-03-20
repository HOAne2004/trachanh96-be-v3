using FluentValidation;
using MediatR;
using Shared.Application.Models;
using Shared.Application.Interfaces;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Stores.Commands
{
    public record UpdateStoreGeneralInfoCommand(
        Guid PublicId,
        string Name,
        string? Description,
        string? ImageUrl,
        string? PhoneNumber,
        string? WifiPassword) : IRequest<Result>;

    public class UpdateStoreGeneralInfoCommandValidator : AbstractValidator<UpdateStoreGeneralInfoCommand>
    {
        public UpdateStoreGeneralInfoCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên cửa hàng không được để trống.")
                .MaximumLength(100).WithMessage("Tên cửa hàng không vượt quá 100 ký tự.");
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả không vượt quá 1000 ký tự.");
            RuleFor(x => x.ImageUrl)
                .MaximumLength(200).WithMessage("URL hình ảnh không vượt quá 200 ký tự.")
                .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("URL hình ảnh không hợp lệ.");
            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Số điện thoại không vượt quá 20 ký tự.")
                .Matches(@"^\+?[0-9\s\-]+$").WithMessage("Số điện thoại chỉ được chứa chữ số, dấu cách, dấu gạch ngang và có thể bắt đầu bằng dấu +.");
            RuleFor(x => x.WifiPassword)
                .MaximumLength(50).WithMessage("Mật khẩu Wi-Fi không vượt quá 50 ký tự.");
        }
    }

    public class UpdateStoreGeneralInfoCommandHandler : IRequestHandler<UpdateStoreGeneralInfoCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IUnitOfWork _unitOfWork;
        public UpdateStoreGeneralInfoCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork)
        {
            _storeRepository = storeRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result> Handle(UpdateStoreGeneralInfoCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
            if (store == null)
            {
                return Result.Failure("Cửa hàng không tồn tại.");
            }
            // Cập nhật thông tin cửa hàng
            try
            {
                // 2. Gọi Behavior của Aggregate Root để cập nhật dữ liệu
                store.UpdateGeneralInfo(
                    request.Name,
                    request.Description,
                    request.ImageUrl,
                    request.PhoneNumber,
                    request.WifiPassword
                );

                // 3. Giao cho UnitOfWork lưu xuống Database
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }
            catch (ArgumentException ex) // Bắt lỗi Validation từ Entity (nếu có)
            {
                return Result.Failure($"Dữ liệu không hợp lệ: {ex.Message}");
            }
        }
    }
}
