using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Orders.Application.Interfaces.ExternalServices;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Orders.Application.Features.Commands
{
    // =========================================================
    // 1. COMMAND
    // =========================================================
    public record SetDineInTableCommand(
        Guid OrderId,
        Guid TableId // Khách quét mã QR tại bàn, App sẽ gửi TableId này lên
    ) : ICommand<Result<Guid>>;

    // =========================================================
    // 2. VALIDATOR
    // =========================================================
    public class SetDineInTableCommandValidator : AbstractValidator<SetDineInTableCommand>
    {
        public SetDineInTableCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.TableId).NotEmpty().WithMessage("Mã bàn không hợp lệ.");
        }
    }

    // =========================================================
    // 3. HANDLER
    // =========================================================
    public class SetDineInTableCommandHandler : IRequestHandler<SetDineInTableCommand, Result<Guid>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IStoreTableService _storeTableService;

        public SetDineInTableCommandHandler(
            IOrderRepository orderRepository,
            IStoreTableService storeTableService)
        {
            _orderRepository = orderRepository;
            _storeTableService = storeTableService;
        }

        public async Task<Result<Guid>> Handle(SetDineInTableCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Tìm đơn hàng
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                    return Result<Guid>.Failure("Không tìm thấy đơn hàng.");

                // 2. Kiểm tra loại đơn
                if (order.OrderType != Orders.Domain.Enums.OrderTypeEnum.DineIn)
                    return Result<Guid>.Failure("Chỉ áp dụng chọn bàn cho loại đơn Ăn tại quán (Dine-in).");

                // 3. Hỏi Store Module xem bàn này có hợp lệ không
                var table = await _storeTableService.GetTableAsync(order.StoreId, request.TableId, cancellationToken);

                if (table == null)
                    return Result<Guid>.Failure("Bàn không tồn tại trong cửa hàng này. Vui lòng quét lại mã QR.");

                if (!table.IsActive)
                    return Result<Guid>.Failure($"Bàn '{table.TableName}' hiện đang ngừng phục vụ. Vui lòng chọn bàn khác.");

                // Tùy chọn nâng cao (nếu cần): Kiểm tra số lượng người có vượt quá sức chứa không?
                // if (order.Items.Sum(i => i.Quantity) > table.SeatCapacity * 2) 
                //    return Result<Guid>.Failure("Số lượng món ăn quá lớn so với sức chứa của bàn.");

                // 4. Cập nhật vào Domain
                order.SetTable(table.TableId);

                // Ghi chú cập nhật từ khách hàng (Ví dụ: "Đem ra bàn số 5 giùm")
                order.UpdateCustomerNotes($"Khách chọn bàn: {table.TableName}");

                return Result<Guid>.Success(order.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(ex.Message);
            }
        }
    }
}
