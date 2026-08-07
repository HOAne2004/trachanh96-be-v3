using Identity.Domain.Entities;
using Identity.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Authorization;

namespace Identity.Infrastructure.Persistence.Seeders;

public static class IdentityDataSeeder
{
    // Truyền thêm IEnumerable<IPermissionProvider> vào để quét
    public static async Task SeedPermissionsAndRolesAsync(
        IdentityDbContext context,
        IEnumerable<IPermissionProvider> permissionProviders)
    {
        // 1. GOM TẤT CẢ QUYỀN TỪ CÁC MODULE KHÁC NHAU
        var allPermissionDefs = permissionProviders.SelectMany(p => p.GetPermissions()).ToList();

        // 2. Chèn Permission nếu chưa tồn tại
        foreach (var def in allPermissionDefs)
        {
            if (!await context.Permissions.AnyAsync(p => p.Id == def.Code))
            {
                context.Permissions.Add(new Permission(def.Code, def.Name, def.Module, def.Description, def.IsSystem));
            }
        }
        await context.SaveChangesAsync();

        // 3. Khởi tạo Admin Role
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");
        if (adminRole == null)
        {
            adminRole = new Role("Admin", "Quản trị viên hệ thống");
            context.Roles.Add(adminRole);
            await context.SaveChangesAsync();
        }

        // 4. Cấp TẤT CẢ quyền cho Admin
        var existingPermissions = await context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        foreach (var def in allPermissionDefs)
        {
            if (!existingPermissions.Contains(def.Code))
            {
                // Cần load entity permission từ context ra để Add
                var permissionEntity = await context.Permissions.FindAsync(def.Code);
                if (permissionEntity != null) adminRole.AddPermission(permissionEntity);
            }
        }

        context.Roles.Update(adminRole);
        await context.SaveChangesAsync();
    }
}