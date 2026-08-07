using Identity.Domain.Events;
using Shared.Domain.Exceptions;
using Shared.Domain.SeedWork;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Identity.Domain.Entities;

public class Role : AuditableEntity<Guid>
{
    public string Name { get; private set; }

    // Tên chuẩn hóa dùng để query db nhanh và chính xác (VD: "SUPER_ADMIN")
    public string NormalizedName { get; private set; }

    public string? Description { get; private set; }
    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    // Bắt buộc phải có constructor rỗng (protected) cho EF Core lúc map dữ liệu
    protected Role()
    {
        Name = null!;
        NormalizedName = null!;
    }

    public Role(string name, string? description = null)
    {
        Id = Guid.CreateVersion7();
        SetName(name);
        Description = description?.Trim();
    }

    // ==========================================
    // LOGIC NGHIỆP VỤ (DOMAIN BEHAVIORS)
    // ==========================================

    public void Rename(string newName)
    {
        if (Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
            return; // Tránh update dư thừa nếu tên không thực sự đổi

        SetName(newName);
    }

    public void ChangeDescription(string? newDescription)
    {
        Description = newDescription?.Trim();
    }
    public static string NormalizeName(string name) => Normalize(name);

    // ==========================================
    // PRIVATE HELPERS
    // ==========================================

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên Role không được để trống.");

        if (name.Length > 50)
            throw new DomainException("Tên Role không được vượt quá 50 ký tự.");

        Name = name.Trim();
        NormalizedName = Normalize(Name);
    }

    /// <summary>
    /// Hàm chuẩn hóa: chuyển thành chữ hoa, loại bỏ dấu tiếng Việt, thay khoảng trắng bằng gạch dưới.
    /// Ví dụ: "Quản Lý Kho" -> "QUAN_LY_KHO". Loại dấu để tránh tạo hai Role trùng ý nghĩa
    /// nhưng khác cách gõ dấu (VD: "Quản Lý Kho" và "Quan Ly Kho").
    /// </summary>
    private static string Normalize(string input)
    {
        var upper = RemoveDiacritics(input.ToUpperInvariant());
        var cleaned = Regex.Replace(upper, @"[^A-Z0-9\s]", "");
        return Regex.Replace(cleaned.Trim(), @"\s+", "_");
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        // "Đ"/"đ" không tách được thành D + dấu phụ qua NormalizationForm.FormD, xử lý riêng.
        return sb.ToString().Replace('Đ', 'D').Replace('đ', 'd');
    }

    public void AddPermission(Permission permission)
    {
        if (permission == null) throw new DomainException("Permission không hợp lệ.");

        if (!_rolePermissions.Any(rp => rp.PermissionId == permission.Id))
        {
            _rolePermissions.Add(new RolePermission(this.Id, permission.Id));
            AddDomainEvent(new RolePermissionsChangedEvent(Id));
        }
    }

    public void RemovePermission(string permissionCode)
    {
        var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionCode);

        if (rolePermission != null)
        {
            _rolePermissions.Remove(rolePermission);
            AddDomainEvent(new RolePermissionsChangedEvent(Id));
        }
    }

    public bool HasPermission(string permissionCode)
    {
        return _rolePermissions.Any(rp => rp.PermissionId == permissionCode);
    }
}