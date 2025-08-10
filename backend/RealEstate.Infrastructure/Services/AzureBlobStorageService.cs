using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using RealEstate.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace RealEstate.Infrastructure.Services;

public sealed class AzureBlobStorageService : IImageStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string _baseUrl;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"] 
            ?? throw new InvalidOperationException("Azure Storage connection string not configured");
        _containerName = configuration["AzureStorage:ContainerName"] ?? "property-images";
        _baseUrl = configuration["AzureStorage:BaseUrl"] ?? string.Empty;
        
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        // Generate unique blob name to avoid conflicts
        var blobName = GenerateUniqueBlobName(fileName);
        var blobClient = containerClient.GetBlobClient(blobName);

        // Upload the image
        await blobClient.UploadAsync(imageStream, new BlobHttpHeaders
        {
            ContentType = contentType,
            CacheControl = "public, max-age=31536000" // 1 year cache
        }, cancellationToken: ct);

        return blobName;
    }

    public async Task<bool> DeleteImageAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
            return response.Value;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Stream> DownloadImageAsync(string blobName, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.DownloadAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task<string> GetImageUrlAsync(string blobName, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(_baseUrl))
        {
            return $"{_baseUrl.TrimEnd('/')}/{_containerName}/{blobName}";
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        return blobClient.Uri.ToString();
    }

    public async Task<bool> ImageExistsAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var response = await blobClient.ExistsAsync(cancellationToken: ct);
            return response.Value;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateUniqueBlobName(string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var randomBytes = new byte[8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var randomString = Convert.ToBase64String(randomBytes).Replace("/", "_").Replace("+", "-").Substring(0, 8);
        
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        
        // Clean the filename to be URL-safe
        var cleanName = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9\-_]", "-");
        
        return $"{cleanName}-{timestamp}-{randomString}{extension}";
    }
}
