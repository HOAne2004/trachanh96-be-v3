using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Enums;
using Shared.Application.Models;
using Shared.Domain.Enums;

namespace Orders.Application.Features.Queries
{
    // ==========================================
    // 1. DTOs (Data Transfer Objects) cho Frontend
    // ==========================================
    public record OrderToppingResponse(
        Guid ToppingId,
        string ToppingName,
        string? ImageUrl,
        decimal Price,
        int Quantity
    );

    public record OrderItemResponse(
        Guid ProductId,
        string ProductName,
        string? ImageUrl,
        SizeEnum Size,
        IceLevelEnum? IceLevel,
        SugarLevelEnum? SugarLevel,
        decimal UnitPrice,
        int Quantity,
        decimal TotalPrice,
        string? Notes,
        List<OrderToppingResponse> Toppings
    );

    public record OrderResponse(
        Guid OrderId,
        string OrderCode,
        Guid StoreId,
        Guid? CustomerId,
        OrderStatusEnum OrderStatus,
        OrderTypeEnum OrderType,
        Guid? PaymentMethodId,
        PaymentStatusEnum PaymentStatus,
        string? AppliedVoucherCode,
        Guid? TableId,
        string? DeliveryAddress,
        string? CustomerNotes,
        // Tiền bạc
        decimal SubTotal,
        decimal ShippingFee,
        decimal DiscountAmount,
        decimal FinalTotal,
        string Currency,
        DateTime CreatedAt,
        List<OrderItemResponse> Items
    );

    // ==========================================
    // 2. Query (Request)
    // ==========================================
    public record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderResponse>>;

    // ==========================================
    // 3. Query Handler (Xử lý Đọc siêu tốc)
    // ==========================================
    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderResponse>>
    {
        private readonly IOrdersDbContext _dbContext;
        public GetOrderByIdQueryHandler(IOrdersDbContext dbContext) => _dbContext = dbContext;

        public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var orderDto = await _dbContext.Orders.AsNoTracking()
                .Where(o => o.Id == request.OrderId)
                .Select(o => new OrderResponse(
                    o.Id, o.OrderCode, o.StoreId, o.CustomerId, o.OrderStatus, o.OrderType,
                    o.PaymentMethodId,
                    o.PaymentStatus,
                    o.AppliedVoucherCode,
                    o.TableId,
                    o.DeliveryDetails != null ? o.DeliveryDetails.Address : null,
                    o.CustomerNotes,
                    o.SubTotal.Amount,
                    o.ShippingFee.Amount,
                    o.DiscountAmount.Amount,
                    o.FinalTotal.Amount,
                    o.Currency, o.CreatedAt,
                    o.Items.Select(i => new OrderItemResponse(
                        i.ProductId, i.ProductName, i.ImageUrl, i.SizeName, i.IceLevel, i.SugarLevel,
                        i.UnitPrice.Amount, i.Quantity, i.TotalPrice.Amount, i.Notes,
                        i.Toppings.Select(t => new OrderToppingResponse(
                            t.ToppingId, t.ToppingName, t.ImageUrl, t.Price.Amount, t.Quantity
                        )).ToList()
                    )).ToList()
                )).FirstOrDefaultAsync(cancellationToken);

            return orderDto == null
                ? Result<OrderResponse>.Failure("Không tìm thấy đơn hàng.")
                : Result<OrderResponse>.Success(orderDto);
        }
    }
}
