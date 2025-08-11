using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using RealEstate.Application.Interfaces;
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

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken ct = default)
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
        await blobClient.UploadAsync(imageStream, new BlobHttpHeaders
        {
            ContentType = contentType,
            CacheControl = "public, max-age=31536000" // 1 year cache
        }, cancellationToken: ct);

        return blobName;
    }

    public async Task<bool> DeleteImageAsync(string blobName, CancellationToken ct = default)
    {
        if (_blobServiceClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not available. Please ensure Azurite is running for development.");
        }

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
        if (_blobServiceClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not available. Please ensure Azurite is running for development.");
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.DownloadAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task<string> GetImageUrlAsync(string blobName, CancellationToken ct = default)
    {
        if (_blobServiceClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not available. Please ensure Azurite is running for development.");
        }

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
        if (_blobServiceClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not available. Please ensure Azurite is running for development.");
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
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        
        return $"{timestamp}-{guid}-{nameWithoutExtension}{extension}";
    }
}
