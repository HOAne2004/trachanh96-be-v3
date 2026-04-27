using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Enums;
using Shared.Application.Models;

namespace Orders.Application.Features.Queries
{
    // ==========================================
    // 1. DTOs
    // ==========================================
    public record StoreDashboardMetricsResponse(
        int TotalOrdersToday,
        decimal TotalRevenueToday,
        int PendingOrdersCount,   // Đơn đang kẹt cần xử lý ngay (Pending, Confirmed, Preparing)
        int ReadyOrdersCount,     // Đơn chờ khách lấy / Shipper tới lấy (Ready)
        int ShippingOrdersCount,  // Đơn đang trên đường giao (Shipping)
        int CancelledOrdersCount
    );

    // ==========================================
    // 2. Query
    // ==========================================
    public record GetStoreDashboardMetricsQuery(Guid StoreId) : IRequest<Result<StoreDashboardMetricsResponse>>;

    // ==========================================
    // 3. Handler
    // ==========================================
    public class GetStoreDashboardMetricsQueryHandler : IRequestHandler<GetStoreDashboardMetricsQuery, Result<StoreDashboardMetricsResponse>>
    {
        private readonly IOrdersDbContext _dbContext;

        public GetStoreDashboardMetricsQueryHandler(IOrdersDbContext dbContext) => _dbContext = dbContext;

        public async Task<Result<StoreDashboardMetricsResponse>> Handle(GetStoreDashboardMetricsQuery request, CancellationToken cancellationToken)
        {
            // Lấy đầu ngày hiện tại (Theo giờ UTC).
            // Thực tế bạn có thể cần truyền múi giờ (Timezone) của cửa hàng từ FE xuống để cắt ngày cho chuẩn.
            var today = DateTime.UtcNow.Date;

            // Kỹ thuật tối ưu: Thay vì kéo toàn bộ dữ liệu, ta chỉ Select đúng 2 cột cần tính toán
            var todayOrders = await _dbContext.Orders.AsNoTracking()
                .Where(o => o.StoreId == request.StoreId && o.CreatedAt >= today)
                .Select(o => new { o.OrderStatus, o.FinalTotal.Amount })
                .ToListAsync(cancellationToken);

            // Tính toán Metrics trong RAM (Vì số lượng đơn/ngày không quá lớn, tính toán in-memory sẽ cực nhanh)
            var totalOrders = todayOrders.Count(o => o.OrderStatus != OrderStatusEnum.Draft); // Bỏ qua nháp

            // Các trạng thái được phép cộng vào Doanh Thu (Bao gồm cả đang giao, đang làm...)
            var validRevenueStatuses = new[] {
                OrderStatusEnum.Pending, OrderStatusEnum.Confirmed, OrderStatusEnum.Preparing,
                OrderStatusEnum.Ready, OrderStatusEnum.Shipping, OrderStatusEnum.Completed
            };

            var totalRevenue = todayOrders
                .Where(o => validRevenueStatuses.Contains(o.OrderStatus))
                .Sum(o => o.Amount);

            var pendingStatuses = new[] { OrderStatusEnum.Pending, OrderStatusEnum.Confirmed, OrderStatusEnum.Preparing };
            var pendingOrdersCount = todayOrders.Count(o => pendingStatuses.Contains(o.OrderStatus));

            var readyOrdersCount = todayOrders.Count(o => o.OrderStatus == OrderStatusEnum.Ready);
            var shippingOrdersCount = todayOrders.Count(o => o.OrderStatus == OrderStatusEnum.Shipping);
            var cancelledOrdersCount = todayOrders.Count(o => o.OrderStatus == OrderStatusEnum.Cancelled);

            var metrics = new StoreDashboardMetricsResponse(
                totalOrders,
                totalRevenue,
                pendingOrdersCount,
                readyOrdersCount,
                shippingOrdersCount,
                cancelledOrdersCount
            );

            return Result<StoreDashboardMetricsResponse>.Success(metrics);
        }
    }
}