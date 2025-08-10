using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;
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
    public async Task<ActionResult<DashboardAnalyticsDto>> GetDashboardAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var analytics = await _analyticsService.GetDashboardAnalyticsAsync(startDate, endDate, ct);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve dashboard analytics", details = ex.Message });
        }
    }

    [HttpGet("properties")]
    public async Task<ActionResult<PropertyAnalyticsDto>> GetPropertyAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? operationType = null,
        CancellationToken ct = default)
    {
        try
        {
            var analytics = await _analyticsService.GetPropertyAnalyticsAsync(startDate, endDate, operationType, ct);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve property analytics", details = ex.Message });
        }
    }

    [HttpGet("owners")]
    public async Task<ActionResult<OwnerAnalyticsDto>> GetOwnerAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var analytics = await _analyticsService.GetOwnerAnalyticsAsync(startDate, endDate, ct);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve owner analytics", details = ex.Message });
        }
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueAnalyticsDto>> GetRevenueAnalytics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? groupBy = "month",
        CancellationToken ct = default)
    {
        try
        {
            var analytics = await _analyticsService.GetRevenueAnalyticsAsync(startDate, endDate, groupBy, ct);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve revenue analytics", details = ex.Message });
        }
    }

    [HttpGet("performance")]
    public async Task<ActionResult<PerformanceMetricsDto>> GetPerformanceMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var metrics = await _analyticsService.GetPerformanceMetricsAsync(startDate, endDate, ct);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve performance metrics", details = ex.Message });
        }
    }
}
