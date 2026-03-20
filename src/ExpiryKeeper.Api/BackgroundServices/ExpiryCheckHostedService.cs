using Microsoft.EntityFrameworkCore;
using MedicineExpiration.Api.Data;
using MedicineExpiration.Api.Services.Notifications;

namespace MedicineExpiration.Api.BackgroundServices;

public class ExpiryCheckHostedService(IServiceScopeFactory scopeFactory, ILogger<ExpiryCheckHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCheckAsync(stoppingToken);
                await WaitUntilNextRunAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // Normal shutdown, exit gracefully
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Expiry check failed, will retry next cycle.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var medicines = await db.Medicines
            .Where(m => m.ExpireDate >= today)
            .ToListAsync(ct);

        var toNotify = medicines
            .Where(m => (m.ExpireDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days <= m.NotifyDaysBefore)
            .ToList();

        foreach (var medicine in toNotify)
        {
            var daysLeft = (medicine.ExpireDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days;
            var message = daysLeft == 0
                ? new NotificationMessage("药品今日到期", $"【{medicine.Name}】今天到期，请尽快处理", "/medicines")
                : new NotificationMessage("药品即将到期", $"【{medicine.Name}】还有 {daysLeft} 天到期", "/medicines");

            try
            {
                await notificationService.SendToUserAsync(medicine.UserOid, message, medicine.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify expiry for medicine {Id}", medicine.Id);
            }
        }

        logger.LogInformation("Expiry check complete. Notified {Count} medicines.", toNotify.Count);
    }

    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        // Run daily at 01:00 UTC (09:00 CST)
        var nextRun = now.Date.AddDays(1).AddHours(1);
        var delay = nextRun - now;
        await Task.Delay(delay, ct);
    }
}
