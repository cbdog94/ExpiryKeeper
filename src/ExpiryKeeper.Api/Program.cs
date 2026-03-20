using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using MedicineExpiration.Api.BackgroundServices;
using MedicineExpiration.Api.Data;
using MedicineExpiration.Api.Services;
using MedicineExpiration.Api.Services.Notifications;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Azure AD authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Notification providers - add new providers here to extend
builder.Services.AddScoped<INotificationProvider, WebPushProvider>();
builder.Services.AddScoped<INotificationProvider, BarkProvider>();
builder.Services.AddScoped<NotificationService>();

// Drug database (stub - replace with real implementation later)
builder.Services.AddScoped<IDrugDatabaseService, StubDrugDatabaseService>();

// OCR
builder.Services.AddScoped<OcrService>();

// Background expiry check
builder.Services.AddHostedService<ExpiryCheckHostedService>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ocr", context =>
    {
        var userId = context.User.FindFirstValue("oid")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS for Vue dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Serve Vue SPA static files in production
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// SPA fallback: all non-API routes serve index.html
app.MapFallbackToFile("index.html");

app.Run();
