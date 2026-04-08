using FluentValidation;
using MediatR;
using Payments.Application.Interfaces;
using Payments.Domain.Entities;
using Shared.Application.Models;

namespace Payments.Application.Features.Commands
{
    public record CreatePaymentLinkCommand(
    Guid OrderId,
    string OrderCode,
    decimal Amount,
    string IpAddress
) : IRequest<Result<string>>;

    public class CreatePaymentLinkCommandValidator : AbstractValidator<CreatePaymentLinkCommand>
    {
        public CreatePaymentLinkCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Mã OrderId không được để trống.");

            RuleFor(x => x.OrderCode)
                .NotEmpty().WithMessage("Mã đơn hàng không được để trống.")
                .MaximumLength(50).WithMessage("Mã đơn hàng không vượt quá 50 ký tự.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Số tiền thanh toán phải lớn hơn 0.");

            RuleFor(x => x.IpAddress)
                .NotEmpty().WithMessage("IP Address không được để trống (VNPay bắt buộc).");
        }
    }

    public class CreatePaymentLinkCommandHandler : IRequestHandler<CreatePaymentLinkCommand, Result<string>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IVnPayService _vnPayService;

        public CreatePaymentLinkCommandHandler(IPaymentRepository paymentRepository, IVnPayService vnPayService)
        {
            _paymentRepository = paymentRepository;
            _vnPayService = vnPayService;
        }

        public async Task<Result<string>> Handle(CreatePaymentLinkCommand request, CancellationToken cancellationToken)
        {
            // Khởi tạo giao dịch chuẩn DDD
            var transaction = PaymentTransaction.Create(
                request.OrderId,
                request.OrderCode,
                request.Amount,
                "VND",
                Domain.Enums.PaymentMethodEnum.VNPay, // Truyền rõ phương thức thanh toán
                Guid.NewGuid()                                 // IdempotencyKey
            );

            // Tạo Link
            string paymentUrl = _vnPayService.CreatePaymentUrl(transaction, request.IpAddress);

            // Đưa vào Repository
            _paymentRepository.Add(transaction);

            await _paymentRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(paymentUrl);
        }
    }
}
