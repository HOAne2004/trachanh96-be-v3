
namespace Catalog.Application.DTOs
{
    public record CategoryDto(
        int Id,
        string Name,
        string Slug,
        int? ParentId,
        int DisplayOrder,
        bool IsActive
    );
}
