namespace Identity.Application.DTOs.Response;

public record RoleAdminDto(
    Guid Id,
    string Name,
    string NormalizedName,
    string? Description,
    int PermissionCount,
    DateTime CreatedAt);