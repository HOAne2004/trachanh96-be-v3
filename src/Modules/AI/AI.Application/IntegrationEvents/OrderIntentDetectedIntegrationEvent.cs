using MediatR;

namespace Shared.Application.IntegrationEvents
{
    // Kế thừa INotification của MediatR để hỗ trợ cơ chế Publish/Subscribe
    public record OrderIntentDetectedIntegrationEvent(
        Guid SessionId,        // Để module Orders biết giỏ hàng này thuộc về phiên chat nào
        string OrderItemsJson, // Chuỗi JSON chứa chi tiết món (lấy từ ActionArguments của Gemini)
        DateTime OccurredOn    // Thời điểm phát sinh sự kiện
    ) : INotification;
}