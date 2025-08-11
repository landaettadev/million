using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Interfaces;
using System.Net.Mime;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/images")]
public class ImageController : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;

    public ImageController(IImageStorageService imageStorageService)
    {
        _imageStorageService = imageStorageService;
    }

    [HttpGet("{imageName}")]
    public async Task<IActionResult> GetImage(string imageName, CancellationToken ct = default)
    {
        try
        {
            // Check if image exists
            if (!await _imageStorageService.ImageExistsAsync(imageName, ct))
            {
                return NotFound($"Image {imageName} not found");
            }

            // Download the image
            var imageStream = await _imageStorageService.DownloadImageAsync(imageName, ct);
            
            // Determine content type based on file extension
            var contentType = GetContentType(imageName);
            
            return File(imageStream, contentType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving image: {ex.Message}");
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
