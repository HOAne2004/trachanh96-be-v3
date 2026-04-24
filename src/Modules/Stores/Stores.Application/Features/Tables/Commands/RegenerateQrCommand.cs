using FluentValidation;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Tables.Commands;

// 1. DTO / Command
public record RegenerateQrCommand(Guid PublicId, int TableId) : ICommand<Result>;

// 2. Validator
public class RegenerateQrCommandValidator : AbstractValidator<RegenerateQrCommand>
{
    public RegenerateQrCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty().WithMessage("ID cửa hàng không hợp lệ.");
        RuleFor(x => x.TableId).GreaterThan(0).WithMessage("ID bàn không hợp lệ.");
    }
}

// 3. Handler
public class RegenerateQrCommandHandler : IRequestHandler<RegenerateQrCommand, Result>
{
    private readonly IStoreRepository _storeRepository;

    public RegenerateQrCommandHandler(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    public async Task<Result> Handle(RegenerateQrCommand request, CancellationToken cancellationToken)
    {
        // Lấy Full Graph Cửa hàng + Khu vực + Bàn
        var store = await _storeRepository.GetAggregateAsync(request.PublicId, cancellationToken);

        if (store == null) return Result.Failure("Cửa hàng không tồn tại hoặc đã bị xóa.");

        try
        {
            // Gọi Behavior đổi mã QR
            store.RegenerateTableQrCode(request.TableId);

            return Result.Success();
        }
        catch (InvalidOperationException ex) 
        {
            return Result.Failure(ex.Message);
        }
        catch (ArgumentException ex) 
        {
            return Result.Failure(ex.Message);
        }
    }
}