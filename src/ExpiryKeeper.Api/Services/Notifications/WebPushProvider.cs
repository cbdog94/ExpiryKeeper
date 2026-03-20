using Microsoft.EntityFrameworkCore;
using MedicineExpiration.Api.Data;
using WebPush;

namespace MedicineExpiration.Api.Services.Notifications;

public class WebPushProvider(AppDbContext db, IConfiguration config, ILogger<WebPushProvider> logger) : INotificationProvider
{
    public string ProviderName => "WebPush";

    public async Task<bool> SendAsync(string userOid, NotificationMessage message, CancellationToken ct = default)
    {
        var subscriptions = await db.PushSubscriptions
            .Where(s => s.UserOid == userOid && s.Provider == Models.PushProvider.WebPush)
            .ToListAsync(ct);

        if (subscriptions.Count == 0) return false;

        var vapidPublicKey = config["Vapid:PublicKey"]!;
        var vapidPrivateKey = config["Vapid:PrivateKey"]!;
        var vapidSubject = config["Vapid:Subject"]!;

        var client = new WebPushClient();
        var vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            title = message.Title,
            body = message.Body,
            url = message.Url
        });

        var allSucceeded = true;
        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WebPush failed for subscription {Id}", sub.Id);
                allSucceeded = false;
            }
        }

        return allSucceeded;
    }
}
