using MedicineExpiration.Api.Data;
using MedicineExpiration.Api.Models;

namespace MedicineExpiration.Api.Services.Notifications;

public class NotificationService(
    IEnumerable<INotificationProvider> providers,
    AppDbContext db,
    ILogger<NotificationService> logger)
{
    public async Task SendToUserAsync(string userOid, NotificationMessage message, int? medicineId = null, CancellationToken ct = default)
    {
        foreach (var provider in providers)
        {
            bool success = false;
            string? error = null;

            try
            {
                success = await provider.SendAsync(userOid, message, ct);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                logger.LogError(ex, "Provider {Provider} threw for user {UserOid}", provider.ProviderName, userOid);
            }

            if (medicineId.HasValue)
            {
                db.NotificationLogs.Add(new NotificationLog
                {
                    MedicineId = medicineId.Value,
                    Provider = provider.ProviderName,
                    Success = success,
                    ErrorMessage = error
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
