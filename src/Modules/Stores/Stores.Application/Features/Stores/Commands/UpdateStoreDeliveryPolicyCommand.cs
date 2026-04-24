using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.ValueObjects;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Stores.Commands
{
    // 1. LÀM PHẲNG DỮ LIỆU (Dùng kiểu nguyên thủy)
    public record UpdateStoreDeliveryPolicyCommand(
        Guid PublicId,
        double? RadiusKm,
        decimal? BaseFeeAmount,
        string? BaseFeeCurrency,
        decimal? FeePerKmAmount,
        string? FeePerKmCurrency) : ICommand<Result>;

    public class UpdateDeliveryPolicyCommandValidator : AbstractValidator<UpdateStoreDeliveryPolicyCommand>
    {
        public UpdateDeliveryPolicyCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");

            RuleFor(x => x.RadiusKm)
                .GreaterThan(0).When(x => x.RadiusKm.HasValue).WithMessage("Bán kính giao hàng phải lớn hơn 0.");

            // Validate trên kiểu số thập phân (decimal) rất dễ dàng và an toàn
            RuleFor(x => x.BaseFeeAmount)
                .GreaterThanOrEqualTo(0).When(x => x.BaseFeeAmount.HasValue).WithMessage("Phí cơ bản phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.FeePerKmAmount)
                .GreaterThanOrEqualTo(0).When(x => x.FeePerKmAmount.HasValue).WithMessage("Phí trên mỗi km phải lớn hơn hoặc bằng 0.");
        }
    }

    public class UpdateDeliveryPolicyCommandHandler : IRequestHandler<UpdateStoreDeliveryPolicyCommand, Result>
    {
        private readonly IStoreRepository _storeRepository;

        public UpdateDeliveryPolicyCommandHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public async Task<Result> Handle(UpdateStoreDeliveryPolicyCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
            if (store == null)
            {
                return Result.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");
            }

            try
            {
                // 2. CHUYỂN ĐỔI SANG VALUE OBJECT
                var newBaseFee = request.BaseFeeAmount.HasValue
                    ? Money.Create(request.BaseFeeAmount.Value, request.BaseFeeCurrency ?? store.BaseShippingFee.Currency)
                    : store.BaseShippingFee;

                var newFeePerKm = request.FeePerKmAmount.HasValue
                    ? Money.Create(request.FeePerKmAmount.Value, request.FeePerKmCurrency ?? store.ShippingFeePerKm.Currency)
                    : store.ShippingFeePerKm;

                // 3. GỌI DOMAIN BEHAVIOR
                store.UpdateDeliveryPolicy(
                    radiusKm: request.RadiusKm ?? store.DeliveryRadiusKm,
                    baseFee: newBaseFee,
                    feePerKm: newFeePerKm);

                return Result.Success();
            }
            // 4. CHỈ BẮT NHỮNG LỖI DO DOMAIN NÉM RA
            catch (ArgumentException ex)
            {
                return Result.Failure($"Dữ liệu cấu hình không hợp lệ: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure($"Lỗi nghiệp vụ: {ex.Message}");
            }
        }
    }
}