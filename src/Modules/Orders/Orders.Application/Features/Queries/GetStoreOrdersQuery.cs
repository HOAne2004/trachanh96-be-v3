
using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Enums;
using Shared.Application.Models;
using Shared.Domain.Enums;

namespace Orders.Application.Features.Queries
{
    public record StoreOrderToppingBrief(
        int Quantity,
        string ToppingName,
        string? ImageUrl
    );

    public record StoreOrderItemBrief(
        int Quantity,
        string ProductName,
        SizeEnum Size,               
        IceLevelEnum? IceLevel,      
        SugarLevelEnum? SugarLevel,  
        string? Notes,               
        List<StoreOrderToppingBrief> Toppings, 
        string? ImageUrl      
    );
    public record StoreOrderSummaryResponse(
        Guid OrderId,
        string OrderCode,
        OrderStatusEnum OrderStatus,
        OrderTypeEnum OrderType,
        decimal FinalTotal,
        string Currency,
        DateTime CreatedAt,
        List<StoreOrderItemBrief> Items);

    public record GetStoreOrdersQuery(
    Guid StoreId,
    OrderStatusEnum? StatusFilter = null, 
    string? SearchTerm = null, 
    int PageIndex = 1,
    int PageSize = 20 
) : IRequest<Result<PagedResult<StoreOrderSummaryResponse>>>;

    public class GetStoreOrdersQueryHandler
    : IRequestHandler<GetStoreOrdersQuery, Result<PagedResult<StoreOrderSummaryResponse>>>
    {
        private readonly IOrdersDbContext _dbContext;

        public GetStoreOrdersQueryHandler(IOrdersDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<PagedResult<StoreOrderSummaryResponse>>> Handle(
            GetStoreOrdersQuery request, CancellationToken cancellationToken)
        {
            // 1. Dựng query cơ bản (Tắt tracking để tối ưu RAM)
            var query = _dbContext.Orders.AsNoTracking()
                .Where(o => o.StoreId == request.StoreId);

            // 2. Lọc theo trạng thái Tab (Pending, Preparing, Ready...)
            if (request.StatusFilter.HasValue)
            {
                query = query.Where(o => o.OrderStatus == request.StatusFilter.Value);
            }

            // 3. Tìm kiếm theo Mã đơn (Dùng ILike của PostgreSQL để không phân biệt hoa/thường)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = $"%{request.SearchTerm.Trim()}%";
                query = query.Where(o => EF.Functions.ILike(o.OrderCode, searchTerm));
            }

            // 4. Đếm tổng để tính số trang
            var totalCount = await query.CountAsync(cancellationToken);

            // 5. Truy vấn Data (OrderBy CreatedAt TĂNG DẦN - Ai đặt trước làm trước)
            var items = await query
                .OrderBy(o => o.PaidAt ?? o.CheckedOutAt)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new StoreOrderSummaryResponse(
                    o.Id,
                    o.OrderCode,
                    o.OrderStatus,
                    o.OrderType,
                    o.FinalTotal.Amount,
                    o.Currency,
                    o.CreatedAt,
                    o.Items.Select(i => new StoreOrderItemBrief(
                        i.Quantity,
                        i.ProductName,
                        i.SizeName,
                        i.IceLevel,
                        i.SugarLevel,
                        i.Notes,
                        i.Toppings.Select(t => new StoreOrderToppingBrief(
                            t.Quantity,
                            t.ToppingName,
                            t.ImageUrl
                        )).ToList(),
                        i.ImageUrl 
                    )).ToList()
                ))
                .ToListAsync(cancellationToken);

            // 6. Đóng gói trả về
            var pagedResult = new PagedResult<StoreOrderSummaryResponse>(
                items, totalCount, request.PageIndex, request.PageSize);

            return Result<PagedResult<StoreOrderSummaryResponse>>.Success(pagedResult);
        }
    }
}
