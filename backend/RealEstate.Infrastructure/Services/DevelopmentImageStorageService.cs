using RealEstate.Application.Interfaces;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Development-friendly image storage service that provides fallback images
/// when Azure Storage is not available in development
/// </summary>
public sealed class DevelopmentImageStorageService : IImageStorageService
{
    private readonly IImageStorageService _azureService;
    private readonly bool _useFallback;

    public DevelopmentImageStorageService(IImageStorageService azureService)
    {
        _azureService = azureService;
        // Check if we're in development mode and Azure Storage is not available
        _useFallback = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken ct = default)
    {
        try
        {
            return await _azureService.UploadImageAsync(imageStream, fileName, contentType, ct);
        }
        catch
        {
            if (_useFallback)
            {
                // Return a fallback image name for development
                return $"dev-{Guid.NewGuid()}-{fileName}";
            }
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            return await _azureService.DeleteImageAsync(blobName, ct);
        }
        catch
        {
            if (_useFallback)
            {
                // In development, just pretend we deleted it
                return true;
            }
            throw;
        }
    }

    public async Task<Stream> DownloadImageAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            return await _azureService.DownloadImageAsync(blobName, ct);
        }
        catch
        {
            if (_useFallback)
            {
                // Return a placeholder image stream
                var placeholderBytes = GetPlaceholderImageBytes();
                return new MemoryStream(placeholderBytes);
            }
            throw;
        }
    }

    public async Task<string> GetImageUrlAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            return await _azureService.GetImageUrlAsync(blobName, ct);
        }
        catch
        {
            if (_useFallback)
            {
                // Return a fallback image URL for development
                return GetFallbackImageUrl(blobName);
            }
            throw;
        }
    }

    public async Task<bool> ImageExistsAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            return await _azureService.ImageExistsAsync(blobName, ct);
        }
        catch
        {
            if (_useFallback)
            {
                // In development, pretend all images exist
                return true;
            }
            throw;
        }
    }

    private string GetFallbackImageUrl(string blobName)
    {
        // Use placeholder images from a reliable source
        var imageIndex = Math.Abs(blobName.GetHashCode()) % 10;
        return $"https://picsum.photos/800/600?random={imageIndex}";
    }

    private byte[] GetPlaceholderImageBytes()
    {
        // Simple 1x1 transparent PNG
        return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
    }
}
