using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;
using RealEstate.Application.Interfaces;
using RealEstate.Application.DTOs;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using RealEstate.Infrastructure;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/images")]
public class AdminImageUploadEndpoints : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;
    private readonly IImageWriteService _imageWriteService;
    private readonly ILogger<AdminImageUploadEndpoints> _logger;
    private readonly IConfiguration _configuration;
    private readonly MongoContext _ctx;

    public AdminImageUploadEndpoints(
        IImageStorageService imageStorageService,
        IImageWriteService imageWriteService,
        ILogger<AdminImageUploadEndpoints> logger,
        IConfiguration configuration,
        MongoContext ctx)
    {
        _imageStorageService = imageStorageService;
        _imageWriteService = imageWriteService;
        _logger = logger;
        _configuration = configuration;
        _ctx = ctx;
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

    public sealed class UploadImageRequest
    {
        public IFormFile File { get; init; } = default!;
        public string PropertyId { get; init; } = string.Empty;
        public bool Enabled { get; init; } = true;
        public int Order { get; init; } = 0;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImageUploadResponseDto>> UploadImage(
        [FromForm] UploadImageRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var file = request.File;
            var propertyId = request.PropertyId;
            var enabled = request.Enabled;
            var order = request.Order;

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

            // Upload original to Azure Blob Storage
            string blobName;
            using (var stream = file.OpenReadStream())
            {
                blobName = await _imageStorageService.UploadImageAsync(
                    stream, file.FileName, file.ContentType, ct);
            }

            // Generate thumbnail (400px width) in memory and upload
            string? thumbnailBlobName = null;
            try
            {
                using var inStream = file.OpenReadStream();
                using var image = SixLabors.ImageSharp.Image.Load(inStream);
                var width = 400;
                var height = (int)Math.Round(image.Height * (width / (double)image.Width));
                image.Mutate(x => x.Resize(width, height));

                using var outStream = new MemoryStream();
                image.SaveAsJpeg(outStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = 80
                });
                outStream.Position = 0;

                var ext = Path.GetExtension(file.FileName);
                var nameNoExt = Path.GetFileNameWithoutExtension(file.FileName);
                var thumbFileName = $"{nameNoExt}-thumb{ext}";
                thumbnailBlobName = await _imageStorageService.UploadImageAsync(outStream, thumbFileName, "image/jpeg", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate thumbnail for {FileName}", file.FileName);
            }

            // Get the public URL (kept for response convenience)
            var imageUrl = await _imageStorageService.GetImageUrlAsync(blobName, ct);

            // Save image metadata to database
            var addImageDto = new AddImageDto(
                PropertyId: propertyId,
                File: blobName,
                Enabled: enabled,
                Order: order,
                FileSize: file.Length,
                ContentType: file.ContentType,
                ThumbnailFile: thumbnailBlobName
            );

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
            _logger.LogError(ex, "Error uploading image for property {PropertyId}", request.PropertyId);
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

    // PURGE: permanently delete blob(s) and metadata
    [HttpDelete("{imageId}/purge")]
    public async Task<ActionResult> PurgeImage(string imageId, CancellationToken ct = default)
    {
        try
        {
            var doc = await _ctx.PropertyImages.Find(x => x.Id == imageId).FirstOrDefaultAsync(ct);
            if (doc is null)
            {
                return NotFound(new { error = "Image not found" });
            }

            // delete blobs
            if (!string.IsNullOrEmpty(doc.File))
            {
                await _imageStorageService.DeleteImageAsync(doc.File, ct);
            }
            if (!string.IsNullOrEmpty(doc.ThumbnailFile))
            {
                await _imageStorageService.DeleteImageAsync(doc.ThumbnailFile, ct);
            }

            // delete metadata
            await _ctx.PropertyImages.DeleteOneAsync(x => x.Id == imageId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging image {ImageId}", imageId);
            return StatusCode(500, new { error = "Failed to purge image", details = ex.Message });
        }
    }

    // FINALIZE SAS upload: create thumbnail and register metadata
    [HttpPost("finalize")]
    public async Task<ActionResult<ImageUploadResponseDto>> FinalizeUpload(
        [FromBody] FinalizeImageUploadRequestDto dto,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.PropertyId) || string.IsNullOrWhiteSpace(dto.BlobName))
            {
                return BadRequest(new { error = "propertyId and blobName are required" });
            }

            // Download original
            using var originalStream = await _imageStorageService.DownloadImageAsync(dto.BlobName, ct);

            // Generate thumbnail
            string? thumbnailBlobName = null;
            try
            {
                using var image = Image.Load(originalStream);
                var width = 400;
                var height = (int)Math.Round(image.Height * (width / (double)image.Width));
                image.Mutate(x => x.Resize(width, height));

                using var outStream = new MemoryStream();
                image.SaveAsJpeg(outStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 });
                outStream.Position = 0;

                var ext = Path.GetExtension(dto.BlobName);
                var nameNoExt = Path.GetFileNameWithoutExtension(dto.BlobName);
                var thumbFileName = $"{nameNoExt}-thumb{ext}";
                thumbnailBlobName = await _imageStorageService.UploadImageAsync(outStream, thumbFileName, "image/jpeg", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate thumbnail for blob {Blob}", dto.BlobName);
            }

            // Save metadata
            var addImageDto = new AddImageDto(
                PropertyId: dto.PropertyId,
                File: dto.BlobName,
                Enabled: dto.Enabled ?? true,
                Order: dto.Order ?? 0,
                FileSize: dto.FileSize ?? 0,
                ContentType: dto.ContentType ?? "image/jpeg",
                ThumbnailFile: thumbnailBlobName
            );
            var imageId = await _imageWriteService.AddAsync(addImageDto, ct);

            var imageUrl = await _imageStorageService.GetImageUrlAsync(dto.BlobName, ct);
            return Ok(new ImageUploadResponseDto
            {
                ImageId = imageId,
                BlobName = dto.BlobName,
                ImageUrl = imageUrl,
                FileName = Path.GetFileName(dto.BlobName),
                FileSize = dto.FileSize ?? 0,
                ContentType = dto.ContentType ?? "image/jpeg"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing image upload for property {PropertyId}", dto.PropertyId);
            return StatusCode(500, new { error = "Failed to finalize image upload", details = ex.Message });
        }
    }

    public sealed class BulkUploadImagesRequest
    {
        public IFormFileCollection Files { get; init; } = default!;
        public string PropertyId { get; init; } = string.Empty;
    }

    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BulkImageUploadResponseDto>> BulkUploadImages(
        [FromForm] BulkUploadImagesRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var files = request.Files;
            var propertyId = request.PropertyId;

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
                    var addImageDto = new AddImageDto(
                        PropertyId: propertyId,
                        File: blobName,
                        Enabled: true,
                        Order: results.Count
                    );

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
            _logger.LogError(ex, "Error in bulk upload for property {PropertyId}", request.PropertyId);
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

public record FinalizeImageUploadRequestDto
{
    public string PropertyId { get; init; } = string.Empty;
    public string BlobName { get; init; } = string.Empty;
    public bool? Enabled { get; init; }
    public int? Order { get; init; }
    public long? FileSize { get; init; }
    public string? ContentType { get; init; }
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
