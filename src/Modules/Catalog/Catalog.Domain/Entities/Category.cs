using Shared.Domain;
using Shared.Domain.ValueObjects;

namespace Catalog.Domain.Entities;

public class Category : AggregateRoot<int>, IAuditableEntity, ISoftDeletableEntity
{
    public string Name { get; private set; }
    public Slug Slug { get; private set; }
    public int? ParentId { get; private set; }

    // Thuộc tính phục vụ UI / Nghiệp vụ
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } 

    // Audit & Soft Delete (Bảo vệ encapsulation)
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    protected Category() {
        Name = null!;
        Slug = null!;
    }

    public Category(string name, int? parentId = null, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên danh mục không được để trống.");

        Name = name.Trim();
        Slug = Slug.Create(Name);
        ParentId = parentId;
        DisplayOrder = displayOrder;
        IsActive = true; 
        IsDeleted = false;
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Tên mới không được để trống.");

        Name = newName.Trim();
        Slug = Slug.Create(Name);
    }

    // Cập nhật Slug bằng tay cho SEO
    public void OverrideSlug(string manualSlug)
    {
        Slug = Slug.CreateManual(manualSlug);
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    public void ToggleActiveStatus(bool isActive)
    {
        IsActive = isActive;
    }

    public void SetParent(Category parentCategory)
    {
        // Null check
        if (parentCategory == null)
            throw new ArgumentNullException(nameof(parentCategory), "Danh mục cha không tồn tại.");

        if (parentCategory.ParentId.HasValue)
            throw new InvalidOperationException("Hệ thống chỉ hỗ trợ danh mục tối đa 2 cấp. Không thể gắn vào danh mục đã là con.");

        if (parentCategory.Id == this.Id)
            throw new InvalidOperationException("Danh mục không thể tự làm cha của chính nó.");

        ParentId = parentCategory.Id;
    }

    public void RemoveParent()
    {
        ParentId = null;
    }

    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        IsActive = false;
    }
}