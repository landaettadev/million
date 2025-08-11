using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;
using RealEstate.Application.Interfaces;
using RealEstate.Application.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/images")]
public class AdminImageUploadEndpoints : ControllerBase
{
    private readonly IAdminImageReadService _adminImageReadService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IImageWriteService _imageWriteService;
    private readonly IValidator<string> _propertyIdValidator;

    public AdminImageUploadEndpoints(
        IAdminImageReadService adminImageReadService,
        IImageStorageService imageStorageService,
        IImageWriteService imageWriteService,
        IValidator<string> propertyIdValidator)
    {
        _adminImageReadService = adminImageReadService;
        _imageStorageService = imageStorageService;
        _imageWriteService = imageWriteService;
        _propertyIdValidator = propertyIdValidator;
    }

    [HttpGet("property/{propertyId}")]
    public async Task<ActionResult<IReadOnlyList<AdminImageDto>>> GetPropertyImages(
        string propertyId,
        CancellationToken ct = default)
    {
        try
        {
            var validationResult = await _propertyIdValidator.ValidateAsync(propertyId, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            var images = await _adminImageReadService.GetByPropertyIdAsync(propertyId, ct);
            return Ok(images);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{imageId}")]
    public async Task<ActionResult> DeleteImage(
        string imageId,
        CancellationToken ct = default)
    {
        try
        {
            // Soft delete image metadata. If it was already deleted or not found, treat as success (idempotent)
            var _ = await _imageWriteService.DeleteAsync(imageId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Upload a single image for a property
    /// </summary>
    /// <param name="request">The image upload request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Image upload result with metadata</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImageUploadResult>> UploadImage(
        [FromForm] ImageUploadRequest request,
        CancellationToken ct = default)
    {
        try
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { error = "No file provided" });

            var validationResult = await _propertyIdValidator.ValidateAsync(request.PropertyId, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            // Store the file in the backing store (Azurite/Azure). Returns blobName
            using var stream = request.File.OpenReadStream();
            var blobName = await _imageStorageService.UploadImageAsync(stream, request.File.FileName, request.File.ContentType, ct);

            // Persist metadata in MongoDB
            var imageId = await _imageWriteService.AddAsync(new AddImageDto(
                PropertyId: request.PropertyId,
                File: blobName,
                Enabled: request.Enabled ?? true,
                Order: request.Order ?? 0,
                FileSize: request.File.Length,
                ContentType: request.File.ContentType
            ), ct);

            // Build public URL
            var url = await _imageStorageService.GetImageUrlAsync(blobName, ct);

            return Ok(new ImageUploadResult
            {
                ImageId = imageId,
                FileName = request.File.FileName,
                Url = url,
                Size = request.File.Length,
                ContentType = request.File.ContentType
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Upload multiple images for a property
    /// </summary>
    /// <param name="request">The bulk image upload request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of image upload results</returns>
    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<List<ImageUploadResult>>> BulkUploadImages(
        [FromForm] BulkImageUploadRequest request,
        CancellationToken ct = default)
    {
        try
        {
            if (request.Files == null || !request.Files.Any())
                return BadRequest(new { error = "No files provided" });

            var validationResult = await _propertyIdValidator.ValidateAsync(request.PropertyId, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            var results = new List<ImageUploadResult>();
            foreach (var file in request.Files)
            {
                using var stream = file.OpenReadStream();
                var blobName = await _imageStorageService.UploadImageAsync(stream, file.FileName, file.ContentType, ct);
                var imageId = await _imageWriteService.AddAsync(new AddImageDto(
                    PropertyId: request.PropertyId,
                    File: blobName,
                    Enabled: true,
                    Order: 0,
                    FileSize: file.Length,
                    ContentType: file.ContentType
                ), ct);
                var url = await _imageStorageService.GetImageUrlAsync(blobName, ct);
                results.Add(new ImageUploadResult
                {
                    ImageId = imageId,
                    FileName = file.FileName,
                    Url = url,
                    Size = file.Length,
                    ContentType = file.ContentType
                });
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Purge (soft delete) multiple images for a property
    /// </summary>
    /// <param name="request">The purge request containing property ID and image IDs</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("purge")]
    [Consumes("application/json")]
    public async Task<ActionResult> PurgeImages(
        [FromBody] PurgeImagesRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var validationResult = await _propertyIdValidator.ValidateAsync(request.PropertyId, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            // Here you could loop and soft-delete image documents, or remove blobs too
            foreach (var id in request.ImageIds)
            {
                await _imageWriteService.DeleteAsync(id, ct);
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Finalize image uploads for a property (no-op in current implementation)
    /// </summary>
    /// <param name="request">The finalize request containing property ID and image IDs</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("finalize")]
    [Consumes("application/json")]
    public async Task<ActionResult> FinalizeImages(
        [FromBody] FinalizeImagesRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var validationResult = await _propertyIdValidator.ValidateAsync(request.PropertyId, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            // No-op in this simplified implementation
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for uploading a single image
/// </summary>
public class ImageUploadRequest
{
    /// <summary>
    /// The image file to upload
    /// </summary>
    public IFormFile? File { get; set; }
    
    /// <summary>
    /// The ID of the property to associate the image with
    /// </summary>
    public string PropertyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the image should be enabled (default: true)
    /// </summary>
    public bool? Enabled { get; set; }
    
    /// <summary>
    /// The display order of the image (default: 0)
    /// </summary>
    public int? Order { get; set; }
}

/// <summary>
/// Request model for bulk uploading multiple images
/// </summary>
public class BulkImageUploadRequest
{
    /// <summary>
    /// The image files to upload
    /// </summary>
    public List<IFormFile>? Files { get; set; }
    
    /// <summary>
    /// The ID of the property to associate the images with
    /// </summary>
    public string PropertyId { get; set; } = string.Empty;
}

/// <summary>
/// Result of an image upload operation
/// </summary>
public class ImageUploadResult
{
    /// <summary>
    /// The unique identifier of the uploaded image
    /// </summary>
    public string ImageId { get; set; } = string.Empty;
    
    /// <summary>
    /// The original filename of the uploaded image
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// The public URL where the image can be accessed
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// The size of the uploaded image in bytes
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// The MIME type of the uploaded image
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Request to purge multiple images
/// </summary>
public class PurgeImagesRequest
{
    /// <summary>
    /// The ID of the property whose images should be purged
    /// </summary>
    public string PropertyId { get; set; } = string.Empty;
    
    /// <summary>
    /// The IDs of the images to purge
    /// </summary>
    public List<string> ImageIds { get; set; } = new();
}

/// <summary>
/// Request to finalize image uploads
/// </summary>
public class FinalizeImagesRequest
{
    /// <summary>
    /// The ID of the property whose images should be finalized
    /// </summary>
    public string PropertyId { get; set; } = string.Empty;
    
    /// <summary>
    /// The IDs of the images to finalize
    /// </summary>
    public List<string> ImageIds { get; set; } = new();
}
