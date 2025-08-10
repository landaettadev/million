namespace RealEstate.Application.DTOs;

public record DashboardAnalyticsDto
{
    public int TotalProperties { get; init; }
    public int TotalOwners { get; init; }
    public int ActiveProperties { get; init; }
    public int PendingProperties { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal MonthlyRevenue { get; init; }
    public decimal YearlyRevenue { get; init; }
    public List<ChartDataPoint> PropertiesByMonth { get; init; } = new();
    public List<ChartDataPoint> RevenueByMonth { get; init; } = new();
    public List<ChartDataPoint> PropertiesByOperationType { get; init; } = new();
}

public record PropertyAnalyticsDto
{
    public int TotalProperties { get; init; }
    public int SaleProperties { get; init; }
    public int RentProperties { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal AverageRentPrice { get; init; }
    public decimal AverageSalePrice { get; init; }
    public List<ChartDataPoint> PropertiesByLocation { get; init; } = new();
    public List<ChartDataPoint> PropertiesByPriceRange { get; init; } = new();
    public List<ChartDataPoint> PropertiesByBedrooms { get; init; } = new();
    public List<ChartDataPoint> PropertiesByBathrooms { get; init; } = new();
}

public record OwnerAnalyticsDto
{
    public int TotalOwners { get; init; }
    public int ActiveOwners { get; init; }
    public int NewOwnersThisMonth { get; init; }
    public decimal AveragePropertiesPerOwner { get; init; }
    public List<ChartDataPoint> OwnersByMonth { get; init; } = new();
    public List<ChartDataPoint> TopOwnersByProperties { get; init; } = new();
}

public record RevenueAnalyticsDto
{
    public decimal TotalRevenue { get; init; }
    public decimal AverageRevenue { get; init; }
    public decimal RevenueGrowth { get; init; }
    public List<ChartDataPoint> RevenueByPeriod { get; init; } = new();
    public List<ChartDataPoint> RevenueByOperationType { get; init; } = new();
    public List<ChartDataPoint> RevenueByLocation { get; init; } = new();
}

public record PerformanceMetricsDto
{
    public double AverageResponseTime { get; init; }
    public int TotalRequests { get; init; }
    public int SuccessfulRequests { get; init; }
    public int FailedRequests { get; init; }
    public double SuccessRate { get; init; }
    public List<ChartDataPoint> ResponseTimeByHour { get; init; } = new();
    public List<ChartDataPoint> RequestsByEndpoint { get; init; } = new();
}

public record ChartDataPoint
{
    public string Label { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public int Count { get; init; }
    public DateTime? Date { get; init; }
}
