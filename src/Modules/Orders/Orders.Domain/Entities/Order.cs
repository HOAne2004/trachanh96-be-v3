using Orders.Domain.Enums;
using Orders.Domain.Events;
using Orders.Domain.ValueObjects;
using Shared.Domain.Enums;
using Shared.Domain.Exceptions;
using Shared.Domain.Interfaces;
using Shared.Domain.SeedWork;
using Shared.Domain.ValueObjects;

namespace Orders.Domain.Entities;

public class Order : AggregateRoot<Guid>, IAuditableEntity
{
    // =========================================================
    // 1. BASIC INFO
    // =========================================================
    public string OrderCode { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid? CustomerId { get; private set; }

    public OrderStatusEnum OrderStatus { get; private set; }
    public OrderTypeEnum OrderType { get; private set; }

    // =========================================================
    // 2. SERVICE INFO
    // =========================================================
    public DeliveryInfo? DeliveryDetails { get; private set; }
    public Guid? TableId { get; private set; }
    public string? CustomerNotes { get; private set; }
    public void SetDeliveryInfo(DeliveryInfo deliveryInfo)
    {
        if (OrderType != OrderTypeEnum.Delivery)
            throw new DomainException("Chỉ đơn giao hàng mới cần thông tin vận chuyển.");

        DeliveryDetails = deliveryInfo;
    }

    public void SetTable(Guid tableId)
    {
        if (OrderType != OrderTypeEnum.DineIn)
            throw new DomainException("Chỉ đơn tại quán mới cần chọn bàn.");

        TableId = tableId;
    }

    public void UpdateCustomerNotes(string? notes)
    {
        // Ghi chú thường chỉ được sửa khi đơn còn đang Nháp
        if (OrderStatus != OrderStatusEnum.Draft)
            throw new DomainException("Chỉ có thể cập nhật ghi chú khi đơn hàng đang ở trạng thái Nháp.");

        CustomerNotes = notes?.Trim();
    }

    // =========================================================
    // 3. PAYMENT & MONEY
    // =========================================================
    public string Currency { get; private set; }
    public Guid? PaymentMethodId { get; private set; }
    public PaymentStatusEnum PaymentStatus { get; private set; }

    public Money SubTotal { get; private set; }
    public Money ShippingFee { get; private set; }
    public Money DiscountAmount { get; private set; }
    public Money FinalTotal { get; private set; }

    // =========================================================
    // 4. VOUCHER SNAPSHOT
    // =========================================================
    public string? AppliedVoucherCode { get; private set; }
    public DiscountTypeEnum? VoucherDiscountType { get; private set; }
    public decimal? VoucherDiscountValue { get; private set; }
    public decimal? VoucherMaxDiscount { get; private set; }
    public decimal? VoucherMinOrderValue { get; private set; }

    // =========================================================
    // 5. TIMESTAMPS
    // =========================================================
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public DateTime? CheckedOutAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public byte[] RowVersion { get; private set; }

    // =========================================================
    // 6. COLLECTIONS
    // =========================================================
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<OrderStatusHistory> _statusHistories = new();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistories => _statusHistories.AsReadOnly();

    protected Order()
    {
        OrderCode = null!;
        Currency = null!;
        SubTotal = null!;
        DiscountAmount = null!;
        FinalTotal = null!;
        ShippingFee = Money.Zero("VND");
        RowVersion = Array.Empty<byte>();
    }

    private Order(Guid storeId, Guid? customerId, string orderCode, OrderTypeEnum type, string currency)
    {
        Id = Guid.NewGuid();
        StoreId = storeId;
        CustomerId = customerId;
        OrderCode = orderCode;
        OrderType = type;
        Currency = currency;

        OrderStatus = OrderStatusEnum.Draft;
        PaymentStatus = PaymentStatusEnum.Pending;

        SubTotal = Money.Zero(currency);
        ShippingFee = Money.Zero(currency);
        DiscountAmount = Money.Zero(currency);
        FinalTotal = Money.Zero(currency);

        RowVersion = Array.Empty<byte>();
    }

    public static Order Create(Guid storeId, Guid? customerId, OrderTypeEnum type, string currency)
    {
        if (type == OrderTypeEnum.Delivery && customerId == null)
            throw new DomainException("Đơn giao hàng yêu cầu thông tin khách hàng.");       

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        var order = new Order(
            storeId,
            customerId,
            GenerateOrderCode(),
            type,
            normalizedCurrency
        );

        order.AddHistory(null, OrderStatusEnum.Draft, "Đơn hàng đã được tạo", customerId);

        order.AddDomainEvent(new OrderCreatedDomainEvent(
            order.Id, order.OrderCode, storeId, customerId));

        return order;
    }

    private static string GenerateOrderCode()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    // =========================================================
    // ITEM MANAGEMENT
    // =========================================================
    public void AddItem(
        Guid productId,
        string productName,
        SizeEnum size,
        IceLevelEnum? ice,
        SugarLevelEnum? sugar,
        Money unitPrice,
        int quantity,
        string? notes,
        List<OrderItemTopping>? toppings = null)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng phải lớn hơn 0.");

        if (OrderStatus != OrderStatusEnum.Draft && OrderStatus != OrderStatusEnum.PaymentFailed)
            throw new InvalidOperationException("Không thể chỉnh sửa mặt hàng.");

        if (unitPrice.Currency != Currency)
            throw new InvalidOperationException("Đơn vị tiền tệ không khớp.");

        var existing = _items.FirstOrDefault(i =>
            i.ProductId == productId &&
            i.SizeName == size &&
            i.IceLevel == ice &&
            i.SugarLevel == sugar &&
            AreToppingsExactMatch(i.Toppings, toppings));

        if (existing != null)
        {
            existing.UpdateQuantity(existing.Quantity + quantity);
        }
        else
        {
            if (_items.Count >= 50)
                throw new InvalidOperationException("Tối đa 50 mặt hàng.");

            var item = new OrderItem(productId, productName, size, ice, sugar, unitPrice, quantity, notes);

            if (toppings != null)
                foreach (var t in toppings) item.AddTopping(t);

            _items.Add(item);
        }

        RecalculateTotals();
    }
    
    public void ClearItems()
    {
        if (OrderStatus != OrderStatusEnum.Draft && OrderStatus != OrderStatusEnum.PaymentFailed)
            throw new InvalidOperationException("Chỉ có thể thay đổi giỏ hàng ở trạng thái Nháp hoặc Lỗi thanh toán.");

        _items.Clear();
        RecalculateTotals(); 
    }
    // =========================================================
    // SHIPPING
    // =========================================================
    public void SetShippingFee(Money fee)
    {
        if (OrderType != OrderTypeEnum.Delivery)
            throw new DomainException("Chỉ đơn giao hàng mới có phí vận chuyển.");

        if (fee.Currency != Currency)
            throw new DomainException("Đơn vị tiền tệ không khớp.");

        ShippingFee = fee;
        RecalculateTotals();
    }

    // =========================================================
    // VOUCHER
    // =========================================================
    public void ApplyVoucher(string code, DiscountTypeEnum type, decimal value, decimal? max, decimal min)
    {
        if (OrderStatus != OrderStatusEnum.Draft)
            throw new DomainException("Chỉ có thể áp dụng voucher khi đơn ở trạng thái Nháp.");

        if (SubTotal.Amount < min)
            throw new DomainException($"Đơn hàng chưa đạt giá trị tối thiểu ({min}) để sử dụng mã giảm giá.");

        AppliedVoucherCode = code;
        VoucherDiscountType = type;
        VoucherDiscountValue = value;
        VoucherMaxDiscount = max;
        VoucherMinOrderValue = min;

        RecalculateTotals();
    }

    public void RemoveVoucher()
    {
        AppliedVoucherCode = null;
        VoucherDiscountType = null;
        VoucherDiscountValue = null;
        VoucherMaxDiscount = null;
        VoucherMinOrderValue = null;
        ClearVoucherState();
        RecalculateTotals();
    }

    // =========================================================
    // CHECKOUT
    // =========================================================
    public void Checkout(Guid paymentMethodId)
    {
        if (OrderStatus != OrderStatusEnum.Draft)
            throw new DomainException("Trạng thái đơn không hợp lệ.");

        if (!_items.Any())
            throw new DomainException("Giỏ hàng rỗng.");

        if (OrderType == OrderTypeEnum.Delivery && DeliveryDetails == null)
            throw new DomainException("Thiếu thông tin giao hàng.");

        if (OrderType == OrderTypeEnum.DineIn && TableId == null)
            throw new DomainException("Chưa chọn bàn.");

        PaymentMethodId = paymentMethodId;
        CheckedOutAt = DateTime.UtcNow;

        ChangeStatus(OrderStatusEnum.AwaitingPayment, "Khách hàng chốt đơn", CustomerId);

        AddDomainEvent(new OrderCheckedOutDomainEvent(
            Id, OrderCode, FinalTotal.Amount, Currency, paymentMethodId));
    }

    public void MarkAsPaid(string transactionId)
    {
        if (PaymentStatus == PaymentStatusEnum.Paid) return;

        if (OrderStatus != OrderStatusEnum.AwaitingPayment)
            throw new InvalidOperationException("Trạng thái đơn không hợp lệ để thanh toán.");

        PaymentStatus = PaymentStatusEnum.Paid;
        PaidAt = DateTime.UtcNow;

        ChangeStatus(OrderStatusEnum.Pending, "Đã thanh toán", null);

        AddDomainEvent(new OrderPaidDomainEvent(
            Id, OrderCode, transactionId, CustomerId, FinalTotal.Amount));
    }
    public void StartPreparing(Guid? staffId)
    {
        if (OrderStatus != OrderStatusEnum.Pending)
            throw new InvalidOperationException("Chỉ đơn đã thanh toán mới được chuẩn bị.");

        ChangeStatus(OrderStatusEnum.Preparing, "Bắt đầu chuẩn bị", staffId);

        AddDomainEvent(new OrderPreparingDomainEvent(Id, OrderCode));
    }
    public void MarkAsReady(Guid? staffId)
    {
        if (OrderStatus != OrderStatusEnum.Preparing)
            throw new InvalidOperationException("Chỉ đơn đang chuẩn bị mới có thể hoàn tất.");

        ChangeStatus(OrderStatusEnum.Ready, "Đơn hàng đã sẵn sàng", staffId);

        AddDomainEvent(new OrderReadyDomainEvent(Id, OrderCode, CustomerId));
    }
    public void Complete()
    {
        if (OrderStatus != OrderStatusEnum.Ready)
            throw new InvalidOperationException("Chỉ những đơn đã sẵn sàng mới có thể hoàn tất.");

        CompletedAt = DateTime.UtcNow;

        ChangeStatus(OrderStatusEnum.Completed, "Hoàn tất", null);
    }
    public void MarkPaymentFailed(string reason)
    {
        if (PaymentStatus == PaymentStatusEnum.Paid) return;

        PaymentStatus = PaymentStatusEnum.Failed;

        ChangeStatus(OrderStatusEnum.PaymentFailed, $"Thanh toán thất bại: {reason}", null);
    }
    public void Cancel(string reason, Guid? by, bool staffOverride = false)
    {
        if (OrderStatus == OrderStatusEnum.Completed)
            throw new InvalidOperationException("Không thể hủy đơn đã hoàn tất.");

        if ((OrderStatus == OrderStatusEnum.Preparing || OrderStatus == OrderStatusEnum.Ready) && !staffOverride)
            throw new InvalidOperationException("Chỉ nhân viên mới có thể hủy đơn trong trạng thái này.");

        CancelledAt = DateTime.UtcNow;

        ChangeStatus(OrderStatusEnum.Cancelled, reason, by);
    }

    // =========================================================
    // INTERNAL
    // =========================================================
    private void RecalculateTotals()
    {
        SubTotal = Money.Create(_items.Sum(i => i.TotalPrice.Amount), Currency);

        if (AppliedVoucherCode != null && VoucherMinOrderValue.HasValue &&
            SubTotal.Amount < VoucherMinOrderValue.Value)
        {
            ClearVoucherState();
        }

        decimal discount = 0;

        if (AppliedVoucherCode != null && VoucherDiscountType.HasValue)
        {
            discount = VoucherDiscountType == DiscountTypeEnum.FixedAmount
                ? VoucherDiscountValue!.Value
                : SubTotal.Amount * (VoucherDiscountValue!.Value / 100m);

            if (VoucherMaxDiscount.HasValue && discount > VoucherMaxDiscount.Value)
                discount = VoucherMaxDiscount.Value;
        }

        if (discount > SubTotal.Amount)
            discount = SubTotal.Amount;

        DiscountAmount = Money.Create(discount, Currency);

        var final = Math.Max(0, SubTotal.Amount + ShippingFee.Amount - DiscountAmount.Amount);
        FinalTotal = Money.Create(final, Currency);
    }
    private void ClearVoucherState()
    {
        AppliedVoucherCode = null;
        VoucherDiscountType = null;
        VoucherDiscountValue = null;
        VoucherMaxDiscount = null;
        VoucherMinOrderValue = null;
    }
    private void ChangeStatus(OrderStatusEnum newStatus, string? reason, Guid? by)
    {
        var old = OrderStatus;
        OrderStatus = newStatus;
        AddHistory(old, newStatus, reason, by);
    }

    private void AddHistory(OrderStatusEnum? from, OrderStatusEnum to, string? reason, Guid? by)
    {
        _statusHistories.Add(new OrderStatusHistory(from, to, reason, by));
    }

    private bool AreToppingsExactMatch(IReadOnlyCollection<OrderItemTopping> existing, List<OrderItemTopping>? incoming)
    {
        if (incoming == null || !incoming.Any())
            return !existing.Any();

        if (existing.Count != incoming.Count)
            return false;

        var e = existing.OrderBy(x => x.ToppingId).Select(x => x.ToppingId);
        var i = incoming.OrderBy(x => x.ToppingId).Select(x => x.ToppingId);

        return e.SequenceEqual(i);
    }
}

public class OrderStatusHistory
{
    public Guid Id { get; private set; }
    public OrderStatusEnum? FromStatus { get; private set; } 
    public OrderStatusEnum ToStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? Reason { get; private set; }
    public Guid? ChangedBy { get; private set; }

    private OrderStatusHistory() { }

    internal OrderStatusHistory(OrderStatusEnum? fromStatus, OrderStatusEnum toStatus, string? reason, Guid? changedBy)
    {
        Id = Guid.NewGuid();
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedAt = DateTime.UtcNow;
        Reason = reason;
        ChangedBy = changedBy;
    }
}

