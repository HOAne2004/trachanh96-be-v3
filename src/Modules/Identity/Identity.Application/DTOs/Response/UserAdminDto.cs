namespace Identity.Application.DTOs.Response;

public record UserAdminDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    IReadOnlyList<string> Roles,
    string Status,
    DateTime CreatedAt
);