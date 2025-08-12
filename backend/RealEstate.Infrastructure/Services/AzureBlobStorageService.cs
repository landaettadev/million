using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealEstate.Application;
using System.Security.Cryptography;
using System.Text;

namespace RealEstate.Infrastructure.Services;

public sealed class AzureBlobStorageService : IImageStorageService
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string _containerName;
    private readonly string _baseUrl;
    private readonly bool _isDevelopmentStorage;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"] 
            ?? throw new InvalidOperationException("Azure Storage connection string not configured");
        _containerName = configuration["AzureStorage:ContainerName"] ?? "property-images";
        _baseUrl = configuration["AzureStorage:BaseUrl"] ?? string.Empty;
        _isDevelopmentStorage = connectionString.Contains("UseDevelopmentStorage=true");
        
        try
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
        catch (Exception ex)
        {
            // In development, we'll handle this gracefully
            if (_isDevelopmentStorage)
            {
                _blobServiceClient = null;
            }
            else
            {
                throw new InvalidOperationException($"Failed to initialize Azure Blob Storage client: {ex.Message}", ex);
            }
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (_blobServiceClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not available. Please ensure Azurite is running for development.");
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        // Generate unique blob name to avoid conflicts
        var blobName = GenerateUniqueBlobName(fileName);
        var blobClient = containerClient.GetBlobClient(blobName);

        // Upload the image
        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
        {
            ContentType = contentType,
            CacheControl = "public, max-age=31536000" // 1 year cache
        }, cancellationToken: ct);

        return blobName;
    }

    public async Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        if (_blobServiceClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not available. Please ensure Azurite is running for development.");
        }

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
            // Return void as per interface
        }
        catch
        {
            // Log error but don't throw as per interface contract
        }
    }

    public string GetImageUrl(string imagePath)
    {
        // URL-encode the blob name to handle spaces/parentheses
        var encodedBlobName = Uri.EscapeDataString(imagePath);

        if (string.IsNullOrEmpty(_baseUrl))
        {
            // In development mode, use Azurite URL
            if (_isDevelopmentStorage)
            {
                return $"http://127.0.0.1:10000/devstoreaccount1/{_containerName}/{encodedBlobName}";
            }

            // Fallback to Azure Storage URL format (production)
            return $"https://millionstorageprod.blob.core.windows.net/{_containerName}/{encodedBlobName}";
        }

        var baseUrlNormalized = _baseUrl.TrimEnd('/');

        // Ensure container segment is present exactly once
        var needsContainer = !baseUrlNormalized.EndsWith($"/{_containerName}", StringComparison.OrdinalIgnoreCase)
                             && !baseUrlNormalized.Contains($"/{_containerName}/", StringComparison.OrdinalIgnoreCase);

        var finalBase = needsContainer
            ? $"{baseUrlNormalized}/{_containerName}"
            : baseUrlNormalized;

        return $"{finalBase}/{encodedBlobName}";
    }

    // Additional helper methods for internal use
    public async Task<bool> ImageExistsAsync(string blobName, CancellationToken ct = default)
    {
        if (_blobServiceClient == null)
        {
            return false;
        }

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
        var randomSuffix = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8))
            .Replace("/", "").Replace("+", "").Replace("=", "");
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        
        return $"{nameWithoutExtension}-{timestamp}-{randomSuffix}{extension}";
    }
}
