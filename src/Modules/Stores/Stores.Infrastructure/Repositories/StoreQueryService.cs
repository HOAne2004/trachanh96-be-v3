using Microsoft.EntityFrameworkCore;
using Shared.Application.Models;
using Stores.Application.DTOs.Responses;
using Stores.Application.Interfaces;
using Stores.Domain.Enums;
using Stores.Infrastructure.Database;

namespace Stores.Infrastructure.Services;

public class StoreQueryService : IStoreQueryService
{
    private readonly StoreDbContext _context;
    public StoreQueryService(StoreDbContext context) => _context = context;

    public async Task<PagedResult<StoreAdminListDto>> GetPagedAdminStoresAsync(
        string? searchTerm, StoreStatusEnum? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Stores.AsNoTracking();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var k = $"%{searchTerm.Trim()}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, k) ||
                EF.Functions.ILike(s.StoreCode, k));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StoreAdminListDto(
                s.PublicId, s.StoreCode, s.Name, s.Status.ToString(), s.FullAddress
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<StoreAdminListDto>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<StoreDetailDto?> GetStoreDetailAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Stores
            .AsNoTracking()
            .Where(s => s.PublicId == publicId)
            .Select(s => new StoreDetailDto
            {
                PublicId = s.PublicId,
                Name = s.Name,
                StoreCode = s.StoreCode,
                FullAddress = s.FullAddress,
                OperatingHours = s.OperatingHours.Select(h => new OperatingHourResponseDto(
                    h.DayOfWeek,
                    h.OpenTime.HasValue ? h.OpenTime.Value.ToString(@"hh\:mm") : "",
                    h.CloseTime.HasValue ? h.CloseTime.Value.ToString(@"hh\:mm") : "",
                    h.IsClosed
                )).ToList(),
                Areas = s.Areas.Select(a => new AreaDto
                {
                    AreaId = a.Id,
                    Name = a.Name,
                    Tables = a.Tables.Select(t => new TableDto(t.Id, t.Name, t.SeatCapacity, t.IsActive)).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AreaResponseDto>> GetStoreAreasAsync(Guid storePublicId, CancellationToken cancellationToken = default)
    {
        return await _context.Stores
            .AsNoTracking()
            .Where(s => s.PublicId == storePublicId)
            .SelectMany(s => s.Areas)
            .Select(a => new AreaResponseDto(
                a.Id,
                a.Name,
                a.IsActive,
                a.Tables.Count() 
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<TableQrResponseDto?> GetTableByQrTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Stores
            .AsNoTracking()
            .Where(s => s.Status == StoreStatusEnum.Active)
            .SelectMany(s => s.Areas, (store, area) => new { Store = store, Area = area })
            .SelectMany(sa => sa.Area.Tables, (sa, table) => new { sa.Store, sa.Area, Table = table })
            .Where(x => x.Table.QrCodeToken == token
                     && x.Table.IsActive
                     && x.Area.IsActive)
            .Select(x => new TableQrResponseDto(
                x.Store.PublicId,
                x.Store.Name,
                x.Area.Id,
                x.Area.Name,
                x.Table.Id,
                x.Table.Name,
                x.Table.SeatCapacity
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}