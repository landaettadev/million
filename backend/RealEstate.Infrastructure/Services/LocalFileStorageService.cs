using RealEstate.Application.Interfaces;

namespace RealEstate.Infrastructure.Services;

public sealed class LocalFileStorageService : IImageStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService()
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        _baseUrl = "http://localhost:5244/images";
        
        // Ensure directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(_basePath, uniqueFileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await imageStream.CopyToAsync(fileStream, cancellationToken);

        return uniqueFileName;
    }

    public Task<string> GetImageUrlAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/{blobName}";
        return Task.FromResult(url);
    }

    public Task<bool> DeleteImageAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(_basePath, blobName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<Stream> DownloadImageAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, blobName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Image file not found: {blobName}");
        }
        
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult((Stream)stream);
    }

    public Task<bool> ImageExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, blobName);
        return Task.FromResult(File.Exists(filePath));
    }
}
