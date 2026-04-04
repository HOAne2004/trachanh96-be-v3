using Orders.Domain.ValueObjects;
using Shared.Domain;
using Shared.Domain.ValueObjects;
using Shared.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Orders.Domain.Entities;

public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public SizeEnum SizeName { get; private set; }
    public IceLevelEnum? IceLevel { get; private set; }
    public SugarLevelEnum? SugarLevel { get; private set; }
    public Money UnitPrice { get; private set; } 
    public Money TotalPrice { get; private set; } 
    public int Quantity { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<OrderItemTopping> _toppings = new();
    public IReadOnlyCollection<OrderItemTopping> Toppings => _toppings.AsReadOnly();

    private OrderItem() { ProductName = null!;  UnitPrice = null!; TotalPrice = null!; }

    internal OrderItem(Guid productId, string productName, SizeEnum sizeName, IceLevelEnum? iceLevel, SugarLevelEnum? sugarLevel, Money unitPrice, int quantity, string? notes)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        SizeName = sizeName;
        IceLevel = iceLevel;
        SugarLevel = sugarLevel;
        UnitPrice = unitPrice;
        Quantity = quantity > 0 ? quantity : throw new ArgumentException("Số lượng phải lớn hơn 0");
        Notes = notes;

        RecalculateTotalPrice(); 
    }

    internal void AddTopping(OrderItemTopping newTopping)
    {
        if (newTopping.Price.Currency != UnitPrice.Currency)
            throw new InvalidOperationException("Tiền topping không khớp");

        var existingTopping = _toppings.FirstOrDefault(t =>
            t.ToppingId == newTopping.ToppingId && t.ToppingName == newTopping.ToppingName);

        if (existingTopping != null)
        {
            // Thay thế bằng VO mới đã cộng dồn số lượng
            _toppings.Remove(existingTopping);
            _toppings.Add(existingTopping.AddQuantity(newTopping.Quantity));
        }
        else
        {
            _toppings.Add(newTopping);
        }

        RecalculateTotalPrice(); // Trigger tính lại
    }

    internal void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0) throw new ArgumentException("Số lượng phải lớn hơn 0");
        Quantity = newQuantity;
        RecalculateTotalPrice(); // Trigger tính lại
    }

    // Lõi tính toán được đóng kín và Persist xuống DB
    [MemberNotNull(nameof(TotalPrice))]
    private void RecalculateTotalPrice()
    {
        var toppingTotal = _toppings.Aggregate(Money.Zero(UnitPrice.Currency), (current, t) => current + t.Price.Multiply(t.Quantity));
        TotalPrice = (UnitPrice + toppingTotal).Multiply(Quantity);
    }
}