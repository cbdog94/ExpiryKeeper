using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineExpiration.Api.Services.Notifications;
using System.Security.Claims;

namespace MedicineExpiration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(NotificationService notificationService) : ControllerBase
{
    private string UserOid => User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("test")]
    public async Task<IActionResult> SendTest([FromBody] TestNotificationRequest? request = null)
    {
        var title = request?.Title ?? "测试通知";
        var body = request?.Body ?? "推送通知配置成功！";

        await notificationService.SendToUserAsync(
            UserOid,
            new NotificationMessage(title, body, "/medicines"));

        return Ok(new { message = "Test notification sent." });
    }

#if DEBUG
    // Dev-only: diagnostics + send push to a specific userOid without auth
    [HttpPost("dev-test")]
    [AllowAnonymous]
    public async Task<IActionResult> DevTest([FromBody] DevTestRequest request,
        [FromServices] Data.AppDbContext db)
    {
        // Show what subscriptions exist for this userOid
        var subs = await db.PushSubscriptions
            .Where(s => s.UserOid == request.UserOid)
            .Select(s => new { s.Provider, s.Endpoint, s.BarkDeviceKey, s.CreatedAt })
            .ToListAsync();

        if (subs.Count == 0)
            return BadRequest(new { error = "No push subscriptions found for this userOid. Did subscribe succeed?", userOid = request.UserOid });

        var errors = new List<string>();
        var firstMedicineId = await db.Medicines
            .Where(m => m.UserOid == request.UserOid)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync();

        try
        {
            await notificationService.SendToUserAsync(
                request.UserOid,
                new NotificationMessage(
                    request.Title ?? "Dev 测试通知",
                    request.Body ?? "后端直接推送测试",
                    "/medicines"),
                firstMedicineId);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        var logs = await db.NotificationLogs
            .OrderByDescending(l => l.SentAt)
            .Take(5)
            .Select(l => new { l.Provider, l.Success, l.ErrorMessage, l.SentAt })
            .ToListAsync();

        return Ok(new { subscriptions = subs, recentLogs = logs, errors });
    }
#endif
}

public record TestNotificationRequest(string? Title, string? Body);
#if DEBUG
public record DevTestRequest(string UserOid, string? Title, string? Body);
#endif
