using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Shared.Application.Features.Uploads.Commands;

// Command nhận vào mảng byte và tên file gốc
public record UploadFileCommand(byte[] FileBytes, string FileName, string SubPath = "uploads") : IRequest<Result<string>>;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.FileBytes).NotEmpty().WithMessage("File không được để trống.");
        RuleFor(x => x.FileName).NotEmpty();
    }
}

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<string>>
{
    private readonly IStorageService _storageService;

    public UploadFileCommandHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<Result<string>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Gọi interface Storage để lưu file
            var fileUrl = await _storageService.UploadFileAsync(request.FileBytes, request.FileName, request.SubPath, cancellationToken);

            return Result<string>.Success(fileUrl);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}