using MediatR;
using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Tables.Queries;

public record GetTableByQrTokenQuery(string Token) : IRequest<Result<TableQrScanResponseDto>>;

public class GetTableByQrTokenQueryHandler : IRequestHandler<GetTableByQrTokenQuery, Result<TableQrScanResponseDto>>
{
    private readonly IStoreQueryService _queryService;

    public GetTableByQrTokenQueryHandler(IStoreQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<TableQrScanResponseDto>> Handle(GetTableByQrTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result<TableQrScanResponseDto>.Failure("Mã QR không hợp lệ.");
        }

        var tableInfo = await _queryService.GetTableByQrTokenAsync(request.Token, cancellationToken);

        if (tableInfo == null)
        {
            return Result<TableQrScanResponseDto>.Failure("Mã QR không tồn tại, cửa hàng đang đóng cửa hoặc bàn đang tạm ngưng phục vụ.");
        }

        return Result<TableQrScanResponseDto>.Success(tableInfo);
    }
}