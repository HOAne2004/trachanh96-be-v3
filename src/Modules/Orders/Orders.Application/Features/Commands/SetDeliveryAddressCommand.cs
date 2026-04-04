using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Orders.Application.Interfaces.ExternalServices;
using Orders.Domain.ValueObjects;
using Shared.Application.Models;
using Shared.Domain.Exceptions;
using Shared.Domain.Utilities;
using Shared.Domain.ValueObjects;

namespace Orders.Application.Features.Commands
{
    // =========================================================
    // 1. COMMAND (DỮ LIỆU TỪ UI GỬI XUỐNG)
    // =========================================================
    public record SetDeliveryAddressCommand(
        Guid OrderId,
        int AddressId // Dùng Int vì Identity.Address của bạn đang xài PK là int
    ) : IRequest<Result<Guid>>;

    // =========================================================
    // 2. VALIDATOR (KIỂM TRA ĐẦU VÀO CƠ BẢN)
    // =========================================================
    public class SetDeliveryAddressCommandValidator : AbstractValidator<SetDeliveryAddressCommand>
    {
        public SetDeliveryAddressCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.AddressId).GreaterThan(0).WithMessage("Mã địa chỉ không hợp lệ.");
        }
    }

    // =========================================================
    // 3. HANDLER (NHẠC TRƯỞNG ĐIỀU PHỐI)
    // =========================================================
    public class SetDeliveryAddressCommandHandler : IRequestHandler<SetDeliveryAddressCommand, Result<Guid>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserAddressService _userAddressService;
        private readonly IStoreDeliveryPolicyService _storeDeliveryPolicyService;

        // Hệ số nhân để ước lượng đường đi thực tế so với đường chim bay (VD: x1.3 hoặc x1.4)
        private const double RoadDistanceMultiplier = 1.3;
        // Mặc định miễn phí km đầu tiên (Nên đưa vào cấu hình Store sau này, ở đây tạm fix cứng để demo)
        private const double BaseDistanceKm = 3.0;

        public SetDeliveryAddressCommandHandler(
            IOrderRepository orderRepository,
            IUserAddressService userAddressService,
            IStoreDeliveryPolicyService storeDeliveryPolicyService)
        {
            _orderRepository = orderRepository;
            _userAddressService = userAddressService;
            _storeDeliveryPolicyService = storeDeliveryPolicyService;
        }

        public async Task<Result<Guid>> Handle(SetDeliveryAddressCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // --- BƯỚC 1: TÌM ĐƠN HÀNG ---
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                    return Result<Guid>.Failure("Không tìm thấy đơn hàng.");

                if (order.OrderType != Domain.Enums.OrderTypeEnum.Delivery)
                    return Result<Guid>.Failure("Chỉ áp dụng địa chỉ giao hàng cho loại đơn Delivery.");

                if (order.CustomerId == null)
                    return Result<Guid>.Failure("Đơn hàng chưa có thông tin khách hàng.");

                // --- BƯỚC 2: HỎI IDENTITY LẤY ĐỊA CHỈ KHÁCH ---
                // Truyền CustomerId vào để bảo mật: Ngăn user A truyền bừa AddressId của user B
                var address = await _userAddressService.GetUserAddressAsync(order.CustomerId.Value, request.AddressId, cancellationToken);
                if (address == null)
                    return Result<Guid>.Failure("Không tìm thấy địa chỉ hoặc địa chỉ không thuộc về khách hàng này.");

                if (!address.Latitude.HasValue || !address.Longitude.HasValue)
                    return Result<Guid>.Failure("Địa chỉ chưa có tọa độ (Latitude/Longitude) để tính phí giao hàng. Vui lòng cập nhật tọa độ.");

                // --- BƯỚC 3: HỎI STORE LẤY CHÍNH SÁCH VẬN CHUYỂN ---
                var storePolicy = await _storeDeliveryPolicyService.GetDeliveryPolicyAsync(order.StoreId, cancellationToken);
                if (storePolicy == null)
                    return Result<Guid>.Failure("Không tìm thấy chính sách giao hàng của cửa hàng.");

                // --- BƯỚC 4: LẤY MÁY TÍNH RA BẤM KHOẢNG CÁCH ---
                double straightLineDistanceKm = GeoDistanceCalculator.CalculateDistanceInKm(
                    storePolicy.Latitude, storePolicy.Longitude,
                    address.Latitude.Value, address.Longitude.Value);

                // Ước lượng khoảng cách đường đi thực tế (nhân hệ số sai số)
                double actualDistanceKm = straightLineDistanceKm * RoadDistanceMultiplier;

                // --- BƯỚC 5: KIỂM TRA BÁN KÍNH CHO PHÉP ---
                if (actualDistanceKm > storePolicy.DeliveryRadiusKm)
                    return Result<Guid>.Failure($"Khoảng cách giao hàng ({actualDistanceKm:F1}km) vượt quá bán kính cho phép của cửa hàng ({storePolicy.DeliveryRadiusKm}km).");

                // --- BƯỚC 6: TÍNH TIỀN SHIP ---
                decimal feeAmount = storePolicy.BaseShippingFee; // Khởi tạo bằng Phí nền (áp dụng cho 3km đầu)

                if (actualDistanceKm > BaseDistanceKm)
                {
                    // Nếu xa hơn 3km, phần dư ra nhân với giá mỗi km
                    decimal extraKm = (decimal)(actualDistanceKm - BaseDistanceKm);
                    feeAmount += extraKm * storePolicy.ShippingFeePerKm;
                }

                // F&B thường làm tròn tiền ship lên hàng nghìn (VD: 16.200đ -> 17.000đ)
                feeAmount = Math.Ceiling(feeAmount / 1000) * 1000;
                var shippingFee = Money.Create(feeAmount, storePolicy.Currency);

                // --- BƯỚC 7: CẬP NHẬT VÀO ĐƠN HÀNG (DOMAIN) ---
                var deliveryInfo = DeliveryInfo.Create(
                    address.RecipientName,
                    address.PhoneNumber,
                    address.FullAddress,
                    pickupTime: null,
                    latitude: address.Latitude.Value,
                    longitude: address.Longitude.Value,
                    providerName: null,
                    trackingId: null
                );

                // Ủy quyền cho Entity Order tự set data và tự cộng dồn FinalTotal
                order.SetDeliveryInfo(deliveryInfo);
                order.SetShippingFee(shippingFee);

                return Result<Guid>.Success(order.Id);
            }
            catch (DomainException ex)
            {
                // Bắt các lỗi do entity Order ném ra (VD: Sai loại tiền tệ)
                return Result<Guid>.Failure(ex.Message);
            }
        }
    }
}
