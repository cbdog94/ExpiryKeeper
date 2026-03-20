namespace MedicineExpiration.Api.Models;

public class Medicine
{
    public int Id { get; set; }
    public string UserOid { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly ExpireDate { get; set; }
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public int NotifyDaysBefore { get; set; } = 7;

    public ICollection<NotificationLog> NotificationLogs { get; set; } = [];
}
