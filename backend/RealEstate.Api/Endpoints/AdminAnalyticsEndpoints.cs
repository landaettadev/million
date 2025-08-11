using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Interfaces;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/analytics")]
public class AdminAnalyticsEndpoints : ControllerBase
{
    private readonly IAdminAnalyticsService _analyticsService;

    public AdminAnalyticsEndpoints(IAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardAnalyticsDto>> GetDashboard(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            DateTime? start = null;
            DateTime? end = null;
            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var s)) start = s;
            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var e)) end = e;

            var dto = await _analyticsService.GetDashboardAnalyticsAsync(start, end, ct);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
