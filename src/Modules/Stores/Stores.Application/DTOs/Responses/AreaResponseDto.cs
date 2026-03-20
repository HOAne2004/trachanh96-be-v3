
namespace Stores.Application.DTOs.Responses
{
    public record AreaResponseDto(
    int AreaId,
    string Name,
    bool IsActive,
    int TableCount 
);
}
