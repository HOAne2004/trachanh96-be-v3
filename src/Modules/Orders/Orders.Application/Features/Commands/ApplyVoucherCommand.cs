using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Orders.Application.Interfaces.ExternalServices;
using Shared.Application.Models;
using Shared.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Features.Commands
{
    // 1. Command (Chỉ nhận duy nhất OrderId và Mã Code)
    public record ApplyVoucherCommand(Guid OrderId, string VoucherCode) : IRequest<Result<Guid>>;

    // 2. Validator
    public class ApplyVoucherCommandValidator : AbstractValidator<ApplyVoucherCommand>
    {
        public ApplyVoucherCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.VoucherCode).NotEmpty().WithMessage("Mã giảm giá không được để trống.");
        }
    }

    // 3. Handler
    public class ApplyVoucherCommandHandler : IRequestHandler<ApplyVoucherCommand, Result<Guid>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IVoucherSnapshotService _voucherService;

        public ApplyVoucherCommandHandler(
            IOrderRepository orderRepository,
            IVoucherSnapshotService voucherService)
        {
            _orderRepository = orderRepository;
            _voucherService = voucherService;
        }

        public async Task<Result<Guid>> Handle(ApplyVoucherCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Lấy đơn hàng
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null) return Result<Guid>.Failure("Không tìm thấy đơn hàng.");

                // 2. Hỏi Module Promotion: "Mã này xài được không?"
                // Thằng Service này bên trong nó sẽ phải check: Có tồn tại không? Hết hạn chưa? Hết lượt chưa?
                var voucher = await _voucherService.GetValidVoucherAsync(request.VoucherCode, cancellationToken);

                if (voucher == null)
                    return Result<Guid>.Failure("Mã giảm giá không tồn tại, đã hết hạn hoặc hết lượt sử dụng.");

                // 3. Ủy quyền cho Domain lưu Snapshot và tính tiền
                order.ApplyVoucher(
                    voucher.VoucherCode,
                    voucher.DiscountType,
                    voucher.DiscountValue,
                    voucher.MaxDiscountAmount,
                    voucher.MinOrderValue
                );

                // TransactionBehavior sẽ tự động SaveChanges()!
                return Result<Guid>.Success(order.Id);
            }
            catch (DomainException ex) // Hứng lỗi "Chưa đạt giá trị tối thiểu"
            {
                return Result<Guid>.Failure(ex.Message);
            }
        }
    }
}
