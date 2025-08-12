using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/properties/{propertyId}/images")]
public sealed class AdminImagesEndpoints : ControllerBase
{
    private readonly IAdminImageReadService _imageReadService;
    private readonly IImageStorageService _imageStorageService;

    public AdminImagesEndpoints(IAdminImageReadService imageReadService, IImageStorageService imageStorageService)
    {
        _imageReadService = imageReadService;
        _imageStorageService = imageStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPropertyImages([FromRoute] string propertyId, CancellationToken ct)
    {
        var images = await _imageReadService.GetPropertyImagesAsync(propertyId, ct);

        // Map to frontend admin API shape
        var payload = images.Select(i =>
        {
            // Prefer explicit external URLs (e.g., picsum) but rebuild Azure URLs to ensure container prefix and base are correct
            string resolvedUrl;
            var hasAbsoluteUrl = !string.IsNullOrWhiteSpace(i.Url) && (i.Url.StartsWith("http://") || i.Url.StartsWith("https://"));
            if (hasAbsoluteUrl && i.Url!.Contains("picsum.photos"))
            {
                resolvedUrl = i.Url!;
            }
            else
            {
                resolvedUrl = _imageStorageService.GetImageUrl(i.FileName);
            }

            return new
            {
                id = i.Id,
                propertyId = propertyId,
                blobName = i.FileName,
                imageUrl = resolvedUrl,
                enabled = i.IsEnabled,
                order = i.Order,
                fileName = i.FileName,
                fileSize = i.Size,
                contentType = i.ContentType,
                createdAt = i.UploadedAt
            };
        });

        return Ok(payload);
    }
}


