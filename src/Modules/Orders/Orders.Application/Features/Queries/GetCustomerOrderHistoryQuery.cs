using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Enums;
using Shared.Application.Models;

namespace Orders.Application.Features.Queries;

public record CustomerOrderSummaryResponse(
    Guid OrderId,
    string OrderCode,
    OrderStatusEnum OrderStatus,
    OrderTypeEnum OrderType,
    decimal FinalTotal,
    string Currency,
    DateTime CreatedAt,
    string? ThumbnailImageUrl
);

public record GetCustomerOrderHistoryQuery(
    Guid CustomerId,
    List<OrderStatusEnum>? StatusFilters = null,
    int PageIndex = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<CustomerOrderSummaryResponse>>>;

public class GetCustomerOrderHistoryQueryHandler : IRequestHandler<GetCustomerOrderHistoryQuery, Result<PagedResult<CustomerOrderSummaryResponse>>>
{
    private readonly IOrdersDbContext _dbContext;

    public GetCustomerOrderHistoryQueryHandler(IOrdersDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PagedResult<CustomerOrderSummaryResponse>>> Handle(GetCustomerOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        // Loại bỏ hoàn toàn Draft khỏi Lịch sử mua hàng
        var query = _dbContext.Orders.AsNoTracking()
            .Where(o => o.CustomerId == request.CustomerId && o.OrderStatus != OrderStatusEnum.Draft);

        if (request.StatusFilters != null && request.StatusFilters.Any())
        {
            query = query.Where(o => request.StatusFilters.Contains(o.OrderStatus));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new CustomerOrderSummaryResponse(
                o.Id, o.OrderCode, o.OrderStatus, o.OrderType,
                o.FinalTotal.Amount, o.Currency, o.CreatedAt, null
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<CustomerOrderSummaryResponse>>.Success(
            new PagedResult<CustomerOrderSummaryResponse>(items, totalCount, request.PageIndex, request.PageSize));
    }
}