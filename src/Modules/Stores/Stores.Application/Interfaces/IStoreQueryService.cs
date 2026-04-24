using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Domain.Enums;

namespace Stores.Application.Interfaces;

public interface IStoreQueryService
{
    Task<PagedResult<StoreAdminListDto>> GetPagedAdminStoresAsync(
        string? searchTerm, StoreStatusEnum? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<StoreDetailDto?> GetStoreDetailAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<List<AreaResponseDto>> GetStoreAreasAsync(Guid storePublicId, CancellationToken cancellationToken = default);
    Task<TableQrResponseDto?> GetTableByQrTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<PagedResult<StoreCustomerListDto>> GetPagedCustomerStoresAsync(
        double? userLat, double? userLng, string? searchTerm, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}