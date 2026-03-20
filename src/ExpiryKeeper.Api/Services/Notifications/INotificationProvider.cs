namespace MedicineExpiration.Api.Services.Notifications;

public record NotificationMessage(string Title, string Body, string? Url = null);

public interface INotificationProvider
{
    string ProviderName { get; }
    Task<bool> SendAsync(string userOid, NotificationMessage message, CancellationToken ct = default);
}
