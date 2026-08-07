namespace Identity.Application.Models;

public sealed record TokenUser(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    Guid SecurityStamp
);