using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineExpiration.Api.Data;
using MedicineExpiration.Api.Models;
using System.Security.Claims;

namespace MedicineExpiration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private string UserOid => User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("vapid-key")]
    [AllowAnonymous]
    public IActionResult GetVapidPublicKey()
        => Ok(new { publicKey = config["Vapid:PublicKey"] });

    [HttpPost("subscribe/webpush")]
    public async Task<IActionResult> SubscribeWebPush([FromBody] WebPushSubscribeRequest request)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserOid == UserOid && s.Provider == PushProvider.WebPush && s.Endpoint == request.Endpoint);

        if (existing is not null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
        }
        else
        {
            db.PushSubscriptions.Add(new PushSubscription
            {
                UserOid = UserOid,
                Provider = PushProvider.WebPush,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth
            });
        }

        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("subscribe/webpush")]
    public async Task<IActionResult> UnsubscribeWebPush([FromBody] UnsubscribeRequest request)
    {
        var sub = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserOid == UserOid && s.Provider == PushProvider.WebPush && s.Endpoint == request.Endpoint);

        if (sub is not null)
        {
            db.PushSubscriptions.Remove(sub);
            await db.SaveChangesAsync();
        }

        return NoContent();
    }

    // Reserved for Bark
    [HttpPost("subscribe/bark")]
    public async Task<IActionResult> SubscribeBark([FromBody] BarkSubscribeRequest request)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserOid == UserOid && s.Provider == PushProvider.Bark);

        if (existing is not null)
        {
            existing.BarkDeviceKey = request.DeviceKey;
            existing.BarkServerUrl = request.ServerUrl ?? "https://api.day.one/push";
        }
        else
        {
            db.PushSubscriptions.Add(new PushSubscription
            {
                UserOid = UserOid,
                Provider = PushProvider.Bark,
                BarkDeviceKey = request.DeviceKey,
                BarkServerUrl = request.ServerUrl ?? "https://api.day.one/push"
            });
        }

        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("subscribe/bark")]
    public async Task<IActionResult> UnsubscribeBark()
    {
        var sub = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserOid == UserOid && s.Provider == PushProvider.Bark);

        if (sub is not null)
        {
            db.PushSubscriptions.Remove(sub);
            await db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        var subs = await db.PushSubscriptions
            .Where(s => s.UserOid == UserOid)
            .Select(s => new { s.Provider, s.Endpoint, s.BarkDeviceKey, s.BarkServerUrl, s.CreatedAt })
            .ToListAsync();

        return Ok(subs);
    }
}

public record WebPushSubscribeRequest(string Endpoint, string P256dh, string Auth);
public record UnsubscribeRequest(string Endpoint);
public record BarkSubscribeRequest(string DeviceKey, string? ServerUrl);
