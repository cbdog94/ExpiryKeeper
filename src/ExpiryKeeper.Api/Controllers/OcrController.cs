using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MedicineExpiration.Api.Services;

namespace MedicineExpiration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OcrController(OcrService ocrService) : ControllerBase
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/heic", "image/gif"];

    [HttpPost("medicine")]
    [RequestSizeLimit(30 * 1024 * 1024)] // 30MB total
    [EnableRateLimiting("ocr")]
    public async Task<IActionResult> Analyze([FromForm] IFormFileCollection images, CancellationToken ct)
    {
        if (images is null || images.Count == 0)
            return BadRequest("No images provided.");

        if (images.Count > 5)
            return BadRequest("Maximum 5 images allowed.");

        var invalid = images.FirstOrDefault(f => !AllowedTypes.Contains(f.ContentType.Split(';')[0].Trim().ToLower()));
        if (invalid is not null)
            return BadRequest($"Unsupported image type: {invalid.ContentType}");

        var streams = images
            .Select(f => (f.OpenReadStream(), f.ContentType))
            .ToList();

        try
        {
            var result = await ocrService.AnalyzeMultipleAsync(streams, ct);
            return Ok(new
            {
                result.Name,
                result.ExpireDate,
                result.Manufacturer,
                result.Category,
                result.RawText
            });
        }
        finally
        {
            foreach (var (stream, _) in streams)
                await stream.DisposeAsync();
        }
    }
}
