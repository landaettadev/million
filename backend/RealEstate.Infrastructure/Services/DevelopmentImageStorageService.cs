using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealEstate.Application;
using System.IO;

namespace RealEstate.Infrastructure.Services;

public sealed class DevelopmentImageStorageService : IImageStorageService
{
    private readonly string _uploadPath;
    private readonly string _baseUrl;
    private readonly ILogger<DevelopmentImageStorageService> _logger;

    public DevelopmentImageStorageService(IConfiguration configuration, ILogger<DevelopmentImageStorageService> logger)
    {
        _uploadPath = configuration["LocalStorage:UploadPath"] ?? "wwwroot/images";
        _baseUrl = configuration["LocalStorage:BaseUrl"] ?? "/images";
        _logger = logger;
        
        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var uniqueFileName = GenerateUniqueFileName(fileName);
        var filePath = Path.Combine(_uploadPath, uniqueFileName);
        
        using var fileStream2 = File.Create(filePath);
        await fileStream.CopyToAsync(fileStream2, ct);
        
        _logger.LogInformation("File uploaded in development mode: {FileName} -> {FilePath}", fileName, filePath);
        return uniqueFileName;
    }

    public Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_uploadPath, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("File deleted in development mode: {FilePath}", filePath);
        }
        
        return Task.CompletedTask;
    }

    public string GetImageUrl(string imagePath)
    {
        return $"{_baseUrl.TrimEnd('/')}/{imagePath}";
    }

    private static string GenerateUniqueFileName(string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        
        return $"{nameWithoutExtension}-{timestamp}{extension}";
    }
}
