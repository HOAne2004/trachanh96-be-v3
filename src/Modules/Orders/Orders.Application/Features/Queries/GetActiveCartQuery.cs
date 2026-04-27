using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Enums;
using Shared.Application.Models;
using Shared.Domain.Enums;

namespace Orders.Application.Features.Queries
{
    // ==========================================
    // 1. DTOs (Chỉ lấy những thông tin cần cho Giỏ hàng)
    // ==========================================
    public record CartItemToppingResponse(Guid ToppingId, string ToppingName, decimal Price, int Quantity);

    public record CartItemResponse(
        Guid ProductId, string ProductName, SizeEnum Size, IceLevelEnum? IceLevel, SugarLevelEnum? SugarLevel,
        decimal UnitPrice, int Quantity, decimal TotalPrice, string? Notes,
        List<CartItemToppingResponse> Toppings
    );

    public record ActiveCartResponse(
        Guid OrderId, string OrderCode, Guid StoreId, Guid? CustomerId, OrderTypeEnum OrderType,
        decimal SubTotal, decimal ShippingFee, decimal DiscountAmount, decimal FinalTotal, string Currency,
        List<CartItemResponse> Items
    );

    // ==========================================
    // 2. Command
    // ==========================================
    // Trả về ActiveCartResponse? (có thể null nếu khách chưa có giỏ hàng)
    public record GetActiveCartQuery(Guid CustomerId, Guid StoreId) : IRequest<Result<ActiveCartResponse?>>;

    // ==========================================
    // 3. Handler
    // ==========================================
    public class GetActiveCartQueryHandler : IRequestHandler<GetActiveCartQuery, Result<ActiveCartResponse?>>
    {
        private readonly IOrdersDbContext _dbContext;

        public GetActiveCartQueryHandler(IOrdersDbContext dbContext) => _dbContext = dbContext;

        public async Task<Result<ActiveCartResponse?>> Handle(GetActiveCartQuery request, CancellationToken cancellationToken)
        {
            var cart = await _dbContext.Orders.AsNoTracking()
                .Where(o => o.CustomerId == request.CustomerId
                         && o.StoreId == request.StoreId
                         && o.OrderStatus == OrderStatusEnum.Draft) // ĐIỀU KIỆN TIÊN QUYẾT
                .Select(o => new ActiveCartResponse(
                    o.Id, o.OrderCode, o.StoreId, o.CustomerId, o.OrderType,
                    o.SubTotal.Amount, o.ShippingFee.Amount, o.DiscountAmount.Amount, o.FinalTotal.Amount, o.Currency,
                    o.Items.Select(i => new CartItemResponse(
                        i.ProductId, i.ProductName, i.SizeName, i.IceLevel, i.SugarLevel,
                        i.UnitPrice.Amount, i.Quantity, i.TotalPrice.Amount, i.Notes,
                        i.Toppings.Select(t => new CartItemToppingResponse(
                            t.ToppingId, t.ToppingName, t.Price.Amount, t.Quantity)).ToList()
                    )).ToList()
                ))
                .FirstOrDefaultAsync(cancellationToken);

            // Trả về null một cách hợp lệ (Success), Frontend nhận null sẽ tự động hỉện "Giỏ hàng rỗng"
            return Result<ActiveCartResponse?>.Success(cart);
        }
    }
}