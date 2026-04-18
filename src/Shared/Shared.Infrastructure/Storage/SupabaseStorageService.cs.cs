using Shared.Application.Interfaces;
using System.IO;

namespace Shared.Infrastructure.Storage;

public class SupabaseStorageService : IStorageService
{
    private readonly Supabase.Client _supabaseClient;
    private const string BUCKET_NAME = "drinking_files";

    public SupabaseStorageService(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string subPath = "uploads", CancellationToken cancellationToken = default)
    {
        if (fileBytes == null || fileBytes.Length == 0)
            throw new ArgumentException("Dữ liệu tệp không hợp lệ.");

        try
        {
            // 1. Tạo tên file duy nhất chống trùng lặp
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = $"{subPath}/{uniqueFileName}";

            // 2. Upload lên Supabase Storage
            await _supabaseClient.Storage
                .From(BUCKET_NAME)
                .Upload(fileBytes, fullPath);

            // 3. Lấy đường dẫn công khai (Public URL)
            var publicUrl = _supabaseClient.Storage
                .From(BUCKET_NAME)
                .GetPublicUrl(fullPath);

            return publicUrl;
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi upload Supabase: {ex.Message}");
        }
    }
}