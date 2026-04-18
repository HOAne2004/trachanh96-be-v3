using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Features.Uploads.Commands;
using Shared.Presentation.Controllers; // Chứa BaseApiController của bạn

namespace Shared.Presentation.Controllers;

public class UploadFileDto
{
    public IFormFile File { get; set; }
}

[Route("api/shared/uploads")]
public class UploadsController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileDto request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn một tệp để tải lên." });

        // 1. Kiểm tra định dạng (Tùy chọn nhưng rất khuyến khích)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Chỉ chấp nhận file ảnh (.jpg, .png, .webp)." });

        // 2. Bóc IFormFile thành mảng byte[] (Chuyển đổi từ Presentation -> Application)
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        // 3. Gửi Command qua MediatR
        var command = new UploadFileCommand(fileBytes, file.FileName, "avatars");
        var result = await Mediator.Send(command);

        // Trả về JSON: { "data": "https://supabase.co/...", "success": true }
        return HandleResult(result);
    }
}