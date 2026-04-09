/// <summary>
/// [BACKGROUND JOB: NHÂN VIÊN DỌN RÁC NỀN]
/// Chức năng: Lớp nền tảng để tạo các luồng chạy ngầm (Daemon) thực thi nhiệm vụ theo chu kỳ.
/// 
/// Cách hoạt động:
/// - Sử dụng vòng lặp vô hạn 'while(!stoppingToken)' kèm theo lệnh Task.Delay để mô phỏng tính năng "Chạy định kỳ mỗi X giờ".
/// - Bọc try/catch toàn cục để đảm bảo nếu có một Job lỗi thì toàn bộ web API không bị sập theo.
/// 
/// Sử dụng: Các Module khác kế thừa class này và viết code vào hàm ExecuteCleanupAsync. 
/// VD: Xóa các OutboxMessage đã xử lý quá 7 ngày, Xóa các mã OTP hết hạn.
/// </summary>

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