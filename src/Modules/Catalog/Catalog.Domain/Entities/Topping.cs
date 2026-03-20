using Shared.Domain;
using Shared.Domain.ValueObjects;

namespace Catalog.Domain.Entities;

public class Topping : AggregateRoot<int>, IAuditableEntity, ISoftDeletableEntity
{
    public string Name { get; private set; }
    public Slug Slug { get; private set; }
    public Money BasePrice { get; private set; } 
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    protected Topping()
    {
        Name = null!;
        Slug = null!;
        BasePrice = null!;
    }

    public Topping(string name, Money basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên Topping không được trống");

        Name = name.Trim();
        Slug = Slug.Create(Name);
        BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
        IsActive = true;
        IsDeleted = false;
    }

    public void UpdateDetails(string name, Money basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên không được trống");

        Name = name.Trim();
        Slug = Slug.Create(Name);
        BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
    }

    public void ToggleActiveStatus(bool isActive) => IsActive = isActive;

    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        IsActive = false;
    }
}