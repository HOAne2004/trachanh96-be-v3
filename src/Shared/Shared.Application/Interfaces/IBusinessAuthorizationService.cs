
namespace Shared.Application.Interfaces
{
    // Dành cho các Service kiểm tra quyền hạn nghiệp vụ (Business Authorization)
    public interface IBusinessAuthorizationService
    {
        Task<bool> AuthorizeAsync<TResource>(TResource resource, string operationName);
    }

    // Cố định các tên hành động để tránh gõ sai chuỗi (Magic strings)
    public static class ResourceOperations
    {
        public const string Read = "Read";
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
    }
}
