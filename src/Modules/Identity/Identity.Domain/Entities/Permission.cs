using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;

namespace Identity.Domain.Entities;

public sealed class Permission : AggregateRoot<string>
{
    public string Name { get; private set; }
    public string Module { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    protected Permission()
    {
        Name = null!;
        Module = null!;
    }

    // isSystem mặc định false: permission tự tạo (không phải seed hệ thống) phải sửa được.
    // Permission hệ thống bắt buộc truyền isSystem: true tường minh khi khởi tạo qua seed data.
    public Permission(string code, string name, string module, string? description = null, bool isSystem = false)
    {
        if (code.Length > 150) throw new DomainException("Mã không quá 150 ký tự");
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Mã Permission không được để trống.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên Permission không được để trống.");
        if (string.IsNullOrWhiteSpace(module)) throw new DomainException("Tên Module không được để trống.");

        Id = code; // Ví dụ: "Products.Create"
        Name = name.Trim();
        Module = module.Trim();
        Description = description?.Trim();
        IsSystem = isSystem;
    }

    // Hành vi cập nhật thông tin (chỉ dùng cho các quyền không phải System)
    public void UpdateDetails(string name, string? description)
    {
        if (IsSystem) throw new DomainException("Không thể chỉnh sửa System Permission.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên Permission không được để trống.");

        Name = name.Trim();
        Description = description?.Trim();
    }
}