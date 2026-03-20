namespace Shared.Application.Models;

public record ErrorResponse(
    string ErrorCode,
    string Message,
    IEnumerable<string>? Details = null
);