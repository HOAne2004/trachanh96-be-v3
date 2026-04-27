
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
        Shipping = 8,
        Completed = 9,
        Cancelled = 10,
    }
}
