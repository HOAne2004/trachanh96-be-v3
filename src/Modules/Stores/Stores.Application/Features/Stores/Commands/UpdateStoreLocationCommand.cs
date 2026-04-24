using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Stores.Commands
{
    public record UpdateStoreLocationCommand(
        Guid PublicId,
        string FullAddress,
        double Latitude,
        double Longitude) : ICommand<Result>;

    public class UpdateStoreLocationCommandValidator : AbstractValidator<UpdateStoreLocationCommand>
    {
        public UpdateStoreLocationCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");

            RuleFor(x => x.FullAddress)
                .NotEmpty().WithMessage("Địa chỉ không được để trống.")
                .MaximumLength(200).WithMessage("Địa chỉ không vượt quá 200 ký tự.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(8.0, 24.0).WithMessage("Vĩ độ phải nằm trong lãnh thổ Việt Nam (8.0 đến 24.0).");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(102.0, 110.0).WithMessage("Kinh độ phải nằm trong lãnh thổ Việt Nam (102.0 đến 110.0).");
        }
    }

    // 3. Handler
    public class UpdateStoreLocationCommandHandler : IRequestHandler<UpdateStoreLocationCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;

        public UpdateStoreLocationCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public async Task<Result> Handle(UpdateStoreLocationCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy Store từ DB
            var store = await _storeRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
            if (store == null)
            {
                return Result.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");
            }

            try
            {
                // 2. Gọi Domain Behavior
                store.UpdateLocation(request.FullAddress, request.Latitude, request.Longitude);

                return Result.Success();
            }
            catch (ArgumentException ex) // Bắt ArgumentException và ArgumentOutOfRangeException
            {
                return Result.Failure($"Dữ liệu vị trí không hợp lệ: {ex.Message}");
            }
            catch (Exception)   
            {
                // Log lỗi hệ thống ở đây nếu cần thiết
                return Result.Failure("Đã xảy ra lỗi không xác định khi cập nhật vị trí cửa hàng.");
            }
        }
    }
}