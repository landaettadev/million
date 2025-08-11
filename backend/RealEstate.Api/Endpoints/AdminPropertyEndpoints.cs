using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;
using RealEstate.Application.Interfaces;
using FluentValidation;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/properties")]
public class AdminPropertyEndpoints : ControllerBase
{
    private readonly IPropertyWriteService _writeService;
    private readonly IAdminPropertyReadService _readService;
    private readonly IAdminOwnerReadService _ownerReadService;
    private readonly IValidator<CreatePropertyDto> _createValidator;
    private readonly IValidator<UpdatePropertyDto> _updateValidator;

    public AdminPropertyEndpoints(
        IPropertyWriteService writeService,
        IAdminPropertyReadService readService,
        IAdminOwnerReadService ownerReadService,
        IValidator<CreatePropertyDto> createValidator,
        IValidator<UpdatePropertyDto> updateValidator)
    {
        _writeService = writeService;
        _readService = readService;
        _ownerReadService = ownerReadService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminPropertyDto>>> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] string? ownerId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] OperationType? operationType,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new AdminPropertySearchQuery(
            SearchTerm: searchTerm,
            OwnerId: ownerId,
            MinPrice: minPrice,
            MaxPrice: maxPrice,
            OperationType: operationType,
            SortBy: sortBy,
            SortDirection: sortDirection,
            Page: page,
            PageSize: pageSize);

        var result = await _readService.SearchAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminPropertyDetailDto>> GetById(string id, CancellationToken ct)
    {
        var detail = await _readService.GetByIdAsync(id, ct);
        if (detail is null) return NotFound();
        return Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreatePropertyDto dto, CancellationToken ct)
    {
        if (dto is null)
        {
            return BadRequest(new { error = "Invalid payload" });
        }

        // Validate the DTO
        var validationResult = await _createValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { 
                error = "One or more validation errors occurred.", 
                errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
            });
        }

        // Validate owner exists
        var owner = await _ownerReadService.GetByIdAsync(dto.OwnerId, ct);
        if (owner is null) return BadRequest(new { error = "Owner not found", ownerId = dto.OwnerId });

        var id = await _writeService.CreateAsync(dto, ct);
        return Created($"/api/admin/properties/{id}", new { id });
    }

    [HttpPut("{id}/featured")]
    public async Task<ActionResult> SetFeatured(string id, [FromBody] SetFeaturedDto dto, CancellationToken ct)
    {
        try
        {
            await _writeService.SetFeaturedAsync(id, dto.IsFeatured, ct);
            return Ok(new { message = "Property featured status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] UpdatePropertyDto dto, CancellationToken ct)
    {
        if (dto is null)
        {
            return BadRequest(new { error = "Invalid payload" });
        }

        // Validate the DTO
        var validationResult = await _updateValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { 
                error = "One or more validation errors occurred.", 
                errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
            });
        }

        var updated = await _writeService.UpdateAsync(id, dto, ct);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _writeService.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        return Ok();
    }

    [HttpGet("{id}/images")]
    public async Task<ActionResult<List<AdminPropertyImageDto>>> GetPropertyImages(string id, CancellationToken ct)
    {
        var detail = await _readService.GetByIdAsync(id, ct);
        if (detail is null) return NotFound();
        return Ok(detail.Images);
    }
}

public record SetFeaturedDto(bool IsFeatured);


