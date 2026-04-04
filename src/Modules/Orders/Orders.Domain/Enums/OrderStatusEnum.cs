
namespace Orders.Domain.Enums
{
    public enum OrderStatusEnum
    {
        Draft = 1,
        AwaitingPayment = 2,
        PaymentFailed = 3,
        Pending = 4,
        Confirmed = 5,
        Preparing = 6,
        Ready= 7,
        Completed = 8,
        Cancelled = 9,
    }
}
