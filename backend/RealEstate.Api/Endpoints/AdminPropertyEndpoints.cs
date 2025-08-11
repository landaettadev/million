using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;
using RealEstate.Application.Interfaces;
using RealEstate.Application.Validators;
using FluentValidation;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/properties")]
public class AdminPropertyEndpoints : ControllerBase
{
    private readonly IAdminPropertyReadService _adminPropertyReadService;
    private readonly IPropertyWriteService _propertyWriteService;
    private readonly IValidator<CreatePropertyDto> _createPropertyValidator;
    private readonly IValidator<UpdatePropertyDto> _updatePropertyValidator;
    private readonly IAdminImageReadService _adminImageReadService;

    public AdminPropertyEndpoints(
        IAdminPropertyReadService adminPropertyReadService,
        IPropertyWriteService propertyWriteService,
        IValidator<CreatePropertyDto> createPropertyValidator,
        IValidator<UpdatePropertyDto> updatePropertyValidator,
        IAdminImageReadService adminImageReadService)
    {
        _adminPropertyReadService = adminPropertyReadService;
        _propertyWriteService = propertyWriteService;
        _createPropertyValidator = createPropertyValidator;
        _updatePropertyValidator = updatePropertyValidator;
        _adminImageReadService = adminImageReadService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminPropertyDto>>> GetProperties(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var query = new AdminPropertySearchQuery(Page: page, PageSize: pageSize);
            var result = await _adminPropertyReadService.SearchAsync(query, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminPropertyDetailDto>> GetPropertyById(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            var property = await _adminPropertyReadService.GetByIdAsync(id, ct);
            if (property == null)
                return NotFound(new { error = "Property not found" });

            return Ok(property);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}/images")]
    public async Task<ActionResult<IReadOnlyList<AdminPropertyImageDto>>> GetPropertyImages(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            // Reuse property read service which already materializes image URLs
            var property = await _adminPropertyReadService.GetByIdAsync(id, ct);
            if (property == null)
                return NotFound(new { error = "Property not found" });

            return Ok(property.Images);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<AdminPropertyDto>> CreateProperty(
        [FromBody] CreatePropertyDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var validationResult = await _createPropertyValidator.ValidateAsync(dto, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            var propertyId = await _propertyWriteService.CreateAsync(dto, ct);
            var property = await _adminPropertyReadService.GetByIdAsync(propertyId, ct);
            
            if (property == null)
                return StatusCode(500, new { error = "Failed to retrieve created property" });

            return CreatedAtAction(nameof(GetPropertyById), new { id = propertyId }, property);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateProperty(
        string id,
        [FromBody] UpdatePropertyDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var validationResult = await _updatePropertyValidator.ValidateAsync(dto, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            await _propertyWriteService.UpdateAsync(id, dto, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Mark as sold (soft-delete)
    [HttpPost("{id}/sold")]
    public async Task<ActionResult> MarkSold(string id, CancellationToken ct = default)
    {
        try
        {
            await _propertyWriteService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Mark as active (undelete)
    [HttpPost("{id}/active")]
    public async Task<ActionResult> MarkActive(string id, CancellationToken ct = default)
    {
        try
        {
            await _propertyWriteService.UndeleteAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProperty(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            await _propertyWriteService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}/featured")]
    public async Task<ActionResult> SetFeatured(
        string id,
        [FromBody] SetFeaturedRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var success = await _propertyWriteService.SetFeaturedAsync(id, request.IsFeatured, ct);
            if (!success)
                return NotFound(new { error = "Property not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class SetFeaturedRequest
{
    public bool IsFeatured { get; set; }
}
