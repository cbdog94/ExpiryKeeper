namespace MedicineExpiration.Api.Models;

public enum PushProvider { WebPush, Bark }

public class PushSubscription
{
    public int Id { get; set; }
    public string UserOid { get; set; } = string.Empty;
    public PushProvider Provider { get; set; }

    // Web Push fields
    public string? Endpoint { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }

    // Bark fields
    public string? BarkDeviceKey { get; set; }
    public string? BarkServerUrl { get; set; } = "https://api.day.one/push";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
