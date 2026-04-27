using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Enums;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Orders.Application.Features.Commands;

public record AutoCancelExpiredOrdersCommand : ICommand<Result<int>>;

public class AutoCancelExpiredOrdersCommandHandler : IRequestHandler<AutoCancelExpiredOrdersCommand, Result<int>>
{
    private readonly IOrdersDbContext _dbContext;
    private readonly IOrderRepository _orderRepository;

    public AutoCancelExpiredOrdersCommandHandler(IOrdersDbContext dbContext, IOrderRepository orderRepository)
    {
        _dbContext = dbContext;
        _orderRepository = orderRepository;
    }

    public async Task<Result<int>> Handle(AutoCancelExpiredOrdersCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var paymentTimeout = now.AddMinutes(-15); // Quá 15' không quét QR
        var pendingTimeout = now.AddMinutes(-5); // Quá 5' quán không bấm nhận đơn

        // CHỈ quét các đơn đã Checkout (AwaitingPayment) hoặc đã trả tiền (Pending)
        var expiredOrders = await _dbContext.Orders
            .Where(o =>
                (o.OrderStatus == OrderStatusEnum.AwaitingPayment && o.CheckedOutAt <= paymentTimeout) ||
                (o.OrderStatus == OrderStatusEnum.Pending && (o.PaidAt ?? o.CheckedOutAt) <= pendingTimeout)
            )
            .ToListAsync(cancellationToken);

        if (!expiredOrders.Any()) return Result<int>.Success(0);

        foreach (var order in expiredOrders)
        {
            string reason = order.OrderStatus == OrderStatusEnum.AwaitingPayment
                ? "Hết thời gian chờ thanh toán."
                : "Quán quá tải, không kịp xác nhận đơn hàng của bạn.";

            order.Cancel(reason, null, true);
        }

        return Result<int>.Success(expiredOrders.Count);
    }
}