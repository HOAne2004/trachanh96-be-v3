using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.BackgroundJobs;

public abstract class BaseCleanupBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly int _runIntervalInHours;

    protected BaseCleanupBackgroundService(ILogger logger, int runIntervalInHours = 24)
    {
        _logger = logger;
        _runIntervalInHours = runIntervalInHours;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"[Cleanup Service] Khởi động dọn rác tự động. Chu kỳ: {_runIntervalInHours} giờ.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Cleanup Service] Có lỗi xảy ra trong quá trình dọn rác.");
            }

            await Task.Delay(TimeSpan.FromHours(_runIntervalInHours), stoppingToken);
        }
    }

    // Các Module sẽ Override hàm này
    protected abstract Task ExecuteCleanupAsync(CancellationToken cancellationToken);
}