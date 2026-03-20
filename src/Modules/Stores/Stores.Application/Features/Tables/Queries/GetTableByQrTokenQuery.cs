using MediatR;
using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Application.Interfaces;

namespace Stores.Application.Features.Tables.Queries;

public record GetTableByQrTokenQuery(string Token) : IRequest<Result<TableQrResponseDto>>;

public class GetTableByQrTokenQueryHandler : IRequestHandler<GetTableByQrTokenQuery, Result<TableQrResponseDto>>
{
    private readonly IStoreQueryService _queryService;

    public GetTableByQrTokenQueryHandler(IStoreQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<TableQrResponseDto>> Handle(GetTableByQrTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result<TableQrResponseDto>.Failure("Mã QR không hợp lệ.");
        }

        var tableInfo = await _queryService.GetTableByQrTokenAsync(request.Token, cancellationToken);

        if (tableInfo == null)
        {
            return Result<TableQrResponseDto>.Failure("Mã QR không tồn tại, cửa hàng đang đóng cửa hoặc bàn đang tạm ngưng phục vụ.");
        }

        return Result<TableQrResponseDto>.Success(tableInfo);
    }
}