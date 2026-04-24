using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Domain.Enums;

namespace Stores.Application.Interfaces;

public interface IStoreQueryService
{
    Task<PagedResult<StoreAdminListDto>> GetPagedAdminStoresAsync(
        string? searchTerm, StoreStatusEnum? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<StoreAdminDetailDto?> GetStoreDetailAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<List<AreaAdminListDto>> GetStoreAreasAsync(Guid storePublicId, CancellationToken cancellationToken = default);
    Task<TableQrScanResponseDto?> GetTableByQrTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<PagedResult<StoreCustomerListDto>> GetPagedCustomerStoresAsync(
        double? userLat, double? userLng, string? searchTerm, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<StoreCustomerDetailDto?> GetCustomerStoreBySlugAsync(string slug, CancellationToken cancellationToken = default);
}