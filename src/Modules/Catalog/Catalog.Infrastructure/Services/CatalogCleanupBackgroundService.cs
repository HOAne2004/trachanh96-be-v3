using Catalog.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.BackgroundJobs;

namespace Catalog.Infrastructure.BackgroundJobs;

public class CatalogCleanupBackgroundService : BaseCleanupBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CatalogCleanupBackgroundService> _logger;

    // Chuyển chu kỳ dọn dẹp là 24h xuống class Base
    public CatalogCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<CatalogCleanupBackgroundService> logger)
        : base(logger, runIntervalInHours: 24)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Luật riêng của Catalog: Giữ lại 30 ngày
        var thresholdDate = DateTime.UtcNow.AddDays(-30);

        _logger.LogInformation($"[Catalog] Bắt đầu xóa vĩnh viễn dữ liệu rác trước {thresholdDate:dd/MM/yyyy}");

        var deletedCategories = await dbContext.Categories
            .Where(x => x.IsDeleted && x.DeletedAt <= thresholdDate)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedProducts = await dbContext.Products
            .Where(x => x.IsDeleted && x.DeletedAt <= thresholdDate)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedToppings = await dbContext.Toppings
            .Where(x => x.IsDeleted && x.DeletedAt <= thresholdDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCategories + deletedProducts + deletedToppings > 0)
            _logger.LogInformation($"[Catalog] Đã dọn dẹp {deletedCategories + deletedProducts + deletedToppings} bản ghi.");
    }
}