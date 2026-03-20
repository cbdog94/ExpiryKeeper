using Microsoft.EntityFrameworkCore;
using MedicineExpiration.Api.Data;

namespace MedicineExpiration.Api.Services.Notifications;

// TODO: Implement Bark push when needed
// Bark API: POST {serverUrl}/{deviceKey}/{title}/{body}
public class BarkProvider(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<BarkProvider> logger) : INotificationProvider
{
    public string ProviderName => "Bark";

    public async Task<bool> SendAsync(string userOid, NotificationMessage message, CancellationToken ct = default)
    {
        var subscriptions = await db.PushSubscriptions
            .Where(s => s.UserOid == userOid && s.Provider == Models.PushProvider.Bark)
            .ToListAsync(ct);

        if (subscriptions.Count == 0) return false;

        var client = httpClientFactory.CreateClient();
        var allSucceeded = true;

        foreach (var sub in subscriptions)
        {
            try
            {
                var serverUrl = sub.BarkServerUrl ?? "https://api.day.one/push";
                var url = $"{serverUrl.TrimEnd('/')}/{sub.BarkDeviceKey}/{Uri.EscapeDataString(message.Title)}/{Uri.EscapeDataString(message.Body)}";
                if (message.Url != null)
                    url += $"?url={Uri.EscapeDataString(message.Url)}";

                var response = await client.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Bark push failed for subscription {Id}: {Status}", sub.Id, response.StatusCode);
                    allSucceeded = false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bark push exception for subscription {Id}", sub.Id);
                allSucceeded = false;
            }
        }

        return allSucceeded;
    }
}
