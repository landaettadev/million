using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/properties")] 
public sealed class AdminPropertiesEndpoints : ControllerBase
{
    private readonly IAdminPropertyService _propertyService;

    public AdminPropertiesEndpoints(IAdminPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    [HttpPut("{id}/featured")] 
    public async Task<IActionResult> SetFeatured([FromRoute] string id, [FromBody] SetFeaturedRequest req, CancellationToken ct)
    {
        var ok = await _propertyService.SetFeaturedAsync(id, req.IsFeatured, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}

public sealed class SetFeaturedRequest { public bool IsFeatured { get; set; } }


