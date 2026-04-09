/// <summary>
/// [PIPELINE BEHAVIOR: CHỐNG XỬ LÝ TRÙNG LẶP (IDEMPOTENCY)]
/// Chức năng: Ngăn chặn tình trạng gửi đúp (Double-Submit) hoặc xử lý lại một Command đã thành công.
/// Cách hoạt động:
/// - Kiểm tra IdempotencyKey của request trong Redis Cache.
/// - Nếu key đã tồn tại -> Ném lỗi từ chối xử lý (tránh trừ tiền 2 lần, tạo 2 đơn hàng).
/// - Nếu key chưa tồn tại -> Lưu key vào Cache (giữ 24h) và cho phép đi tiếp vào Handler.
/// Sử dụng: Áp dụng cho các Command implement interface IIdempotentCommand (thường là Thanh toán, Trừ kho, Tạo đơn).
/// </summary>

using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Application.Interfaces;

namespace Shared.Application.Behaviors;

public class IdempotentCommandBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand<TResponse>
{
    private readonly IDistributedCache _cache;

    public IdempotentCommandBehavior(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 1. Tạo một cái tên duy nhất cho ổ khóa trong Redis
        // Ví dụ: Idempotency:CheckoutOrderCommand:5f4d...
        var cacheKey = $"Idempotency:{typeof(TRequest).Name}:{request.IdempotencyKey}";

        // 2. Check xem chìa khóa đã tồn tại chưa
        var existingRequest = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(existingRequest))
        {
            // Nếu đã tồn tại -> Chặn đứng ngay lập tức!
            // Tùy thiết kế, bạn có thể quăng Exception để Middleware bắt lại và trả về HTTP 409 Conflict
            throw new InvalidOperationException("Yêu cầu này đã được xử lý hoặc đang trong quá trình xử lý. Vui lòng không gửi lại.");
        }

        // 3. Nếu chưa tồn tại -> Lưu khóa này vào Cache (Giữ trong 24 giờ)
        await _cache.SetStringAsync(cacheKey, "Processed", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        }, cancellationToken);

        // 4. Cho phép đi tiếp vào Handler (Checkout / Webhook...)
        return await next();
    }
}