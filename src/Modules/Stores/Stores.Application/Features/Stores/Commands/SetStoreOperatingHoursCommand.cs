using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.DTOs.Requests;
using Stores.Application.Interfaces;
using Stores.Domain.Entities;

namespace Stores.Application.Features.Stores.Commands
{
    public record SetStoreOperatingHoursCommand(
            Guid PublicId,
            List<OperatingHourRequestDto> OperatingHours) : IRequest<Result>;

    public class SetStoreOperatingHoursCommandValidator : AbstractValidator<SetStoreOperatingHoursCommand>
    {
        public SetStoreOperatingHoursCommandValidator()
        {
            RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");

            RuleFor(x => x.OperatingHours)
                .NotEmpty().WithMessage("Danh sách giờ hoạt động không được để trống.")
                .Must(operatingHours => operatingHours.Select(oh => oh.DayOfWeek).Distinct().Count() == operatingHours.Count)
                .WithMessage("Mỗi ngày trong tuần chỉ được phép có một khung giờ hoạt động.");

            RuleForEach(x => x.OperatingHours).ChildRules(hours =>
            {
                hours.RuleFor(h => h.OpenTime)
                    .NotNull().When(h => !h.IsClosed)
                    .WithMessage("Phải chọn giờ mở cửa nếu quán hoạt động.");

                hours.RuleFor(h => h.CloseTime)
                    .NotNull().When(h => !h.IsClosed)
                    .WithMessage("Phải chọn giờ đóng cửa nếu quán hoạt động.");
            });
        }
    }

    public class SetStoreOperatingHoursCommandHandler : IRequestHandler<SetStoreOperatingHoursCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SetStoreOperatingHoursCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork)
        {
            _storeRepository = storeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(SetStoreOperatingHoursCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
            if (store == null) return Result.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");

            try
            {
                // Map dữ liệu truyền thẳng IsClosed từ Frontend
                var operatingHourConfigs = request.OperatingHours
                    .Select(oh => new OperatingHourConfig(oh.DayOfWeek, oh.OpenTime, oh.CloseTime, oh.IsClosed))
                    .ToList();

                store.SetOperatingHours(operatingHourConfigs);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (ArgumentException ex) { return Result.Failure($"Dữ liệu không hợp lệ: {ex.Message}"); }
            catch (InvalidOperationException ex) { return Result.Failure($"Lỗi nghiệp vụ: {ex.Message}"); }
        }
    }
}
