using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;
using RealEstate.Application.Interfaces;
using RealEstate.Application.Validators;
using FluentValidation;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/owners")]
public class AdminOwnerEndpoints : ControllerBase
{
    private readonly IAdminOwnerReadService _adminOwnerReadService;
    private readonly IValidator<CreateOwnerDto> _createOwnerValidator;
    private readonly IValidator<UpdateOwnerDto> _updateOwnerValidator;

    public AdminOwnerEndpoints(
        IAdminOwnerReadService adminOwnerReadService,
        IValidator<CreateOwnerDto> createOwnerValidator,
        IValidator<UpdateOwnerDto> updateOwnerValidator)
    {
        _adminOwnerReadService = adminOwnerReadService;
        _createOwnerValidator = createOwnerValidator;
        _updateOwnerValidator = updateOwnerValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOwnerDto>>> GetOwners(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var query = new AdminOwnerSearchQuery(Page: page, PageSize: pageSize);
            var result = await _adminOwnerReadService.SearchAsync(query, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminOwnerDto>> GetOwnerById(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            var owner = await _adminOwnerReadService.GetByIdAsync(id, ct);
            if (owner == null)
                return NotFound(new { error = "Owner not found" });

            return Ok(owner);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<OwnerDetailDto>> CreateOwner(
        [FromBody] CreateOwnerDto createOwnerDto,
        CancellationToken ct = default)
    {
        try
        {
            var validationResult = await _createOwnerValidator.ValidateAsync(createOwnerDto, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            // TODO: Implement owner creation service
            return StatusCode(501, new { error = "Owner creation not yet implemented" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OwnerDetailDto>> UpdateOwner(
        string id,
        [FromBody] UpdateOwnerDto updateOwnerDto,
        CancellationToken ct = default)
    {
        try
        {
            var validationResult = await _updateOwnerValidator.ValidateAsync(updateOwnerDto, ct);
            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            // TODO: Implement owner update service
            return StatusCode(501, new { error = "Owner update not yet implemented" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOwner(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement owner deletion service
            return StatusCode(501, new { error = "Owner deletion not yet implemented" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
