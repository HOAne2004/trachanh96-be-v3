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

    public async Task<PagedResult<StoreCustomerListDto>> GetPagedCustomerStoresAsync(
        double? userLat, double? userLng, string? searchTerm, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Stores
            .AsNoTracking()
            .Include(s => s.OperatingHours)
            .Where(s => s.Status == StoreStatusEnum.Active); // Khách chỉ thấy quán đang hoạt động

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var k = $"%{searchTerm.Trim()}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, k) ||
                EF.Functions.ILike(s.FullAddress, k)); // ILike của PostgreSQL hỗ trợ tìm kiếm không phân biệt hoa thường
        }

        // Lấy danh sách thô từ DB lên
        var activeStores = await query.ToListAsync(cancellationToken);

        // Xử lý múi giờ: Railway thường dùng Linux (Asia/Ho_Chi_Minh), Local máy tính thường dùng Windows (SE Asia Standard Time)
        var timeZoneId = TimeZoneInfo.GetSystemTimeZones().Any(x => x.Id == "SE Asia Standard Time")
            ? "SE Asia Standard Time"
            : "Asia/Ho_Chi_Minh";

        var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        var today = nowVn.DayOfWeek;
        var timeNow = nowVn.TimeOfDay;

        var resultList = new List<StoreCustomerListDto>();

        foreach (var store in activeStores)
        {
            // 1. Tính toán khoảng cách nếu user cấp quyền vị trí
            double? distance = null;
            if (userLat.HasValue && userLng.HasValue)
            {
                distance = CalculateHaversineDistance(userLat.Value, userLng.Value, store.Latitude, store.Longitude);
            }

            // 2. Xác định giờ hoạt động hôm nay
            var todaySchedule = store.OperatingHours.FirstOrDefault(h => h.DayOfWeek == today);
            bool isOpenNow = false;
            string? closingTime = null;

            if (todaySchedule != null && !todaySchedule.IsClosed && todaySchedule.OpenTime.HasValue && todaySchedule.CloseTime.HasValue)
            {
                closingTime = todaySchedule.CloseTime.Value.ToString(@"hh\:mm");

                if (todaySchedule.OpenTime.Value <= todaySchedule.CloseTime.Value)
                {
                    isOpenNow = timeNow >= todaySchedule.OpenTime.Value && timeNow <= todaySchedule.CloseTime.Value;
                }
                else // Xử lý ca xuyên đêm
                {
                    isOpenNow = timeNow >= todaySchedule.OpenTime.Value || timeNow <= todaySchedule.CloseTime.Value;
                }
            }

            resultList.Add(new StoreCustomerListDto(
                store.PublicId,
                store.Name,
                store.FullAddress,
                store.ImageUrl,
                distance.HasValue ? Math.Round(distance.Value, 1) : null,
                isOpenNow,
                closingTime
            ));
        }

        // 3. Sắp xếp: Ưu tiên quán Đang Mở Cửa lên đầu -> Tiếp theo ưu tiên Khoảng cách gần nhất
        var orderedList = resultList
            .OrderByDescending(s => s.IsOpenNow)
            .ThenBy(s => s.DistanceKm ?? double.MaxValue)
            .ToList();

        // 4. Phân trang trên bộ nhớ
        var totalCount = orderedList.Count;
        var pagedItems = orderedList
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<StoreCustomerListDto>(pagedItems, totalCount, pageIndex, pageSize);
    }

    // --- Các hàm phụ trợ ---

    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371; // Bán kính trung bình của Trái Đất (km)
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        return r * c;
    }

    private double ToRadians(double angle) => Math.PI * angle / 180.0;
}