using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;
using RealEstate.Application.Interfaces;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/owners")]
public class AdminOwnerEndpoints : ControllerBase
{
    private readonly IOwnerWriteService _ownerWriteService;
    private readonly IAdminOwnerReadService _ownerReadService;

    public AdminOwnerEndpoints(IOwnerWriteService ownerWriteService, IAdminOwnerReadService ownerReadService)
    {
        _ownerWriteService = ownerWriteService;
        _ownerReadService = ownerReadService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOwnerDto>>> SearchOwners(
        [FromQuery] string? searchTerm,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new AdminOwnerSearchQuery(
            SearchTerm: searchTerm,
            SortBy: string.IsNullOrWhiteSpace(sortBy) ? "CreatedAt" : sortBy,
            SortDirection: string.IsNullOrWhiteSpace(sortDirection) ? "desc" : sortDirection,
            Page: page < 1 ? 1 : page,
            PageSize: pageSize <= 0 ? 20 : pageSize
        );

        var result = await _ownerReadService.SearchAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminOwnerDetailDto>> GetOwnerById(string id, CancellationToken ct)
    {
        var owner = await _ownerReadService.GetByIdAsync(id, ct);
        if (owner is null) return NotFound();
        return Ok(owner);
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateOwner([FromBody] CreateOwnerDto dto, CancellationToken ct)
    {
        var id = await _ownerWriteService.CreateAsync(dto, ct);
        return Created($"/api/admin/owners/{id}", new { id });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateOwner(string id, [FromBody] UpdateOwnerDto dto, CancellationToken ct)
    {
        var updated = await _ownerWriteService.UpdateAsync(id, dto, ct);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOwner(string id, CancellationToken ct)
    {
        var deleted = await _ownerWriteService.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        return Ok();
    }
}


