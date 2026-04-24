using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.DTOs.Requests;
using Stores.Application.Interfaces;
using Stores.Domain.Entities;

namespace Stores.Application.Features.Stores.Commands
{

    public record CreateStoreCommand(
        string StoreCode,
        string Name,
        string FullAddress,
        double Latitude,
        double Longitude,
        List<OperatingHourRequestDto> OperatingHours
    ) : ICommand<Result<Guid>>;

    public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
    {
        public CreateStoreCommandValidator()
        {
            RuleFor(x => x.StoreCode)
                .NotEmpty().WithMessage("Mã cửa hàng không được để trống.")
                .MaximumLength(20).WithMessage("Mã cửa hàng không vượt quá 20 ký tự.")
                .Matches("^[a-zA-Z0-9-]+$").WithMessage("Mã cửa hàng chỉ được chứa chữ cái không dấu, số và dấu gạch ngang (-).");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên cửa hàng không được để trống.")
                .MaximumLength(100).WithMessage("Tên cửa hàng không vượt quá 100 ký tự.");

            RuleFor(x => x.FullAddress)
                .NotEmpty().WithMessage("Địa chỉ không được để trống.")
                .MaximumLength(200).WithMessage("Địa chỉ không vượt quá 200 ký tự.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(8.0, 24.0).WithMessage("Vĩ độ phải nằm trong lãnh thổ Việt Nam (8.0 đến 24.0).");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(102.0, 110.0).WithMessage("Kinh độ phải nằm trong lãnh thổ Việt Nam (102.0 đến 110.0).");

            RuleFor(x => x.OperatingHours)
                .NotEmpty().WithMessage("Giờ hoạt động không được để trống.")
                .Must(oh => oh.Select(o => o.DayOfWeek).Distinct().Count() == oh.Count)
                .WithMessage("Mỗi ngày trong tuần chỉ được cấu hình tối đa một lần.");

            RuleForEach(x => x.OperatingHours).ChildRules(hours =>
            {
                hours.RuleFor(h => h.OpenTime)
                    .NotNull().When(h => !h.IsClosed)
                    .WithMessage("Phải chọn giờ mở cửa nếu quán hoạt động trong ngày này.");

                hours.RuleFor(h => h.CloseTime)
                    .NotNull().When(h => !h.IsClosed)
                    .WithMessage("Phải chọn giờ đóng cửa nếu quán hoạt động trong ngày này.");
            });
        }
    }

    public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Result<Guid>>
    {
        private readonly IStoreRepository _storeRepository;

        public CreateStoreCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public async Task<Result<Guid>> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra trùng StoreCode
            if (await _storeRepository.ExistsByStoreCodeAsync(request.StoreCode, cancellationToken))
            {
                return Result<Guid>.Failure($"Mã cửa hàng '{request.StoreCode}' đã tồn tại.");
            }

            try
            {
                // 2. Khởi tạo Aggregate Root
                var store = Store.Create(
                    Guid.NewGuid(),
                    request.StoreCode,
                    request.Name,
                    request.FullAddress,
                    request.Latitude,
                    request.Longitude
                );

                // 3. Map cấu hình giờ hoạt động
                var schedule = request.OperatingHours
                    .Select(oh => new OperatingHourConfig(oh.DayOfWeek, oh.OpenTime, oh.CloseTime, oh.IsClosed))
                    .ToList();

                store.SetOperatingHours(schedule);

                _storeRepository.Add(store);

                return Result<Guid>.Success(store.PublicId);
            }
            catch (ArgumentException ex)
            {
                return Result<Guid>.Failure($"Dữ liệu không hợp lệ: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Result<Guid>.Failure($"Lỗi nghiệp vụ: {ex.Message}");
            }
        }
    }
}