using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Application.Features.Commands;

namespace Orders.Infrastructure.BackgroundJobs;

public class OrderAutoCancelWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public OrderAutoCancelWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Gửi lệnh quét tự động
                await mediator.Send(new AutoCancelExpiredOrdersCommand(), stoppingToken);
            }

            // Chờ 1 phút trước khi quét lượt tiếp theo
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}