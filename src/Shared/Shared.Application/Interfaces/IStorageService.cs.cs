namespace Shared.Application.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string subPath = "uploads", CancellationToken cancellationToken = default);
    }
}
