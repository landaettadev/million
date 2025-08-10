using RealEstate.Application.DTOs;

namespace RealEstate.Application.Interfaces;

public interface IAdminAnalyticsService
{
    Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
    Task<PropertyAnalyticsDto> GetPropertyAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, string? operationType = null, CancellationToken ct = default);
    Task<OwnerAnalyticsDto> GetOwnerAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
    Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate, string groupBy = "month", CancellationToken ct = default);
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
}
