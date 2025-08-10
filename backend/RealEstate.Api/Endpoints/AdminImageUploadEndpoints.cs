using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Interfaces;
using RealEstate.Application.DTOs;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/images")]
public class AdminImageUploadEndpoints : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;
    private readonly IImageWriteService _imageWriteService;
    private readonly ILogger<AdminImageUploadEndpoints> _logger;
    private readonly IConfiguration _configuration;

    public AdminImageUploadEndpoints(
        IImageStorageService imageStorageService,
        IImageWriteService imageWriteService,
        ILogger<AdminImageUploadEndpoints> logger,
        IConfiguration configuration)
    {
        _imageStorageService = imageStorageService;
        _imageWriteService = imageWriteService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("presign")]
    public ActionResult<PresignUploadResponseDto> GetPresignedUploadUrl(
        [FromBody] PresignUploadRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
        {
            return BadRequest(new { error = "fileName and contentType are required" });
        }

        var connectionString = _configuration["AzureStorage:ConnectionString"];
        var containerName = _configuration["AzureStorage:ContainerName"] ?? "property-images";
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return StatusCode(500, new { error = "Azure Storage connection not configured" });
        }

        var containerClient = new BlobContainerClient(connectionString, containerName);
        containerClient.CreateIfNotExists();

        var blobName = GenerateUniqueBlobName(request.FileName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var expiresIn = TimeSpan.FromSeconds(request.ExpiresSeconds <= 0 ? 900 : request.ExpiresSeconds);
        var expiresAt = DateTimeOffset.UtcNow.Add(expiresIn);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiresAt
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        var uri = blobClient.GenerateSasUri(sasBuilder);

        return Ok(new PresignUploadResponseDto
        {
            BlobName = blobName,
            UploadUrl = uri.ToString(),
            ExpiresAt = expiresAt,
            Method = "PUT",
        });
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ImageUploadResponseDto>> UploadImage(
        [FromForm] IFormFile file,
        [FromForm] string propertyId,
        [FromForm] bool enabled = true,
        [FromForm] int order = 0,
        CancellationToken ct = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            if (string.IsNullOrEmpty(propertyId))
            {
                return BadRequest(new { error = "Property ID is required" });
            }

            // Validate file type
            if (!IsValidImageFile(file))
            {
                return BadRequest(new { error = "Invalid file type. Only images are allowed." });
            }

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { error = "File size too large. Maximum size is 10MB." });
            }

            // Upload to Azure Blob Storage
            string blobName;
            using (var stream = file.OpenReadStream())
            {
                blobName = await _imageStorageService.UploadImageAsync(
                    stream, file.FileName, file.ContentType, ct);
            }

            // Get the public URL
            var imageUrl = await _imageStorageService.GetImageUrlAsync(blobName, ct);

            // Save image metadata to database
            var addImageDto = new AddImageDto
            {
                PropertyId = propertyId,
                File = blobName, // Store blob name for reference
                Enabled = enabled,
                Order = order
            };

            var imageId = await _imageWriteService.AddAsync(addImageDto, ct);

            _logger.LogInformation("Image uploaded successfully. ImageId: {ImageId}, BlobName: {BlobName}", imageId, blobName);

            return Ok(new ImageUploadResponseDto
            {
                ImageId = imageId,
                BlobName = blobName,
                ImageUrl = imageUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for property {PropertyId}", propertyId);
            return StatusCode(500, new { error = "Failed to upload image", details = ex.Message });
        }
    }

    [HttpDelete("{imageId}")]
    public async Task<ActionResult> DeleteImage(string imageId, CancellationToken ct = default)
    {
        try
        {
            // Get image metadata to find blob name
            // This would require a read service method to get image by ID
            // For now, we'll assume the imageWriteService.DeleteAsync handles this
            
            var deleted = await _imageWriteService.DeleteAsync(imageId, ct);
            if (!deleted)
            {
                return NotFound(new { error = "Image not found" });
            }

            // Note: In a production system, you might want to also delete from Azure Blob Storage
            // This would require getting the blob name from the database first
            
            _logger.LogInformation("Image deleted successfully. ImageId: {ImageId}", imageId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
            return StatusCode(500, new { error = "Failed to delete image", details = ex.Message });
        }
    }

    [HttpPost("bulk-upload")]
    public async Task<ActionResult<BulkImageUploadResponseDto>> BulkUploadImages(
        [FromForm] IFormFileCollection files,
        [FromForm] string propertyId,
        CancellationToken ct = default)
    {
        try
        {
            if (files == null || !files.Any())
            {
                return BadRequest(new { error = "No files provided" });
            }

            if (string.IsNullOrEmpty(propertyId))
            {
                return BadRequest(new { error = "Property ID is required" });
            }

            var results = new List<ImageUploadResultDto>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    if (file.Length == 0)
                    {
                        errors.Add($"File {file.FileName} is empty");
                        continue;
                    }

                    if (!IsValidImageFile(file))
                    {
                        errors.Add($"File {file.FileName} is not a valid image");
                        continue;
                    }

                    if (file.Length > 10 * 1024 * 1024)
                    {
                        errors.Add($"File {file.FileName} is too large (max 10MB)");
                        continue;
                    }

                    // Upload to Azure Blob Storage
                    string blobName;
                    using (var stream = file.OpenReadStream())
                    {
                        blobName = await _imageStorageService.UploadImageAsync(
                            stream, file.FileName, file.ContentType, ct);
                    }

                    // Get the public URL
                    var imageUrl = await _imageStorageService.GetImageUrlAsync(blobName, ct);

                    // Save image metadata to database
                    var addImageDto = new AddImageDto
                    {
                        PropertyId = propertyId,
                        File = blobName,
                        Enabled = true,
                        Order = results.Count
                    };

                    var imageId = await _imageWriteService.AddAsync(addImageDto, ct);

                    results.Add(new ImageUploadResultDto
                    {
                        ImageId = imageId,
                        BlobName = blobName,
                        ImageUrl = imageUrl,
                        FileName = file.FileName,
                        FileSize = file.Length,
                        ContentType = file.ContentType,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file {FileName} for property {PropertyId}", file.FileName, propertyId);
                    errors.Add($"Failed to upload {file.FileName}: {ex.Message}");
                }
            }

            _logger.LogInformation("Bulk upload completed. Success: {SuccessCount}, Errors: {ErrorCount}", results.Count, errors.Count);

            return Ok(new BulkImageUploadResponseDto
            {
                Results = results,
                Errors = errors,
                TotalFiles = files.Count,
                SuccessfulUploads = results.Count,
                FailedUploads = errors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk upload for property {PropertyId}", propertyId);
            return StatusCode(500, new { error = "Failed to process bulk upload", details = ex.Message });
        }
    }

    private static bool IsValidImageFile(IFormFile file)
    {
        if (file == null) return false;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var mimeType = file.ContentType.ToLowerInvariant();

        return allowedExtensions.Contains(extension) && allowedMimeTypes.Contains(mimeType);
    }

    private static string GenerateUniqueBlobName(string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var randomString = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var cleanName = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9\-_]", "-");
        return $"{cleanName}-{timestamp}-{randomString}{extension}";
    }
}

public record ImageUploadResponseDto
{
    public string ImageId { get; init; } = string.Empty;
    public string BlobName { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = string.Empty;
}

public record PresignUploadRequestDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public int ExpiresSeconds { get; init; } = 900;
}

public record PresignUploadResponseDto
{
    public string BlobName { get; init; } = string.Empty;
    public string UploadUrl { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public string Method { get; init; } = "PUT";
}

public record BulkImageUploadResponseDto
{
    public List<ImageUploadResultDto> Results { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public int TotalFiles { get; init; }
    public int SuccessfulUploads { get; init; }
    public int FailedUploads { get; init; }
}

public record ImageUploadResultDto
{
    public string ImageId { get; init; } = string.Empty;
    public string BlobName { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public bool Success { get; init; }
}
