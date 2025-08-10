using MongoDB.Driver;
using MongoDB.Bson;
using RealEstate.Application.DTOs;
using RealEstate.Application.Interfaces;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly MongoContext _ctx;

    public AdminAnalyticsService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        var filter = Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false);
        
        if (startDate.HasValue && endDate.HasValue)
        {
            filter &= Builders<PropertyDocument>.Filter.Gte(x => x.CreatedAt, startDate.Value) &
                     Builders<PropertyDocument>.Filter.Lte(x => x.CreatedAt, endDate.Value);
        }

        var totalProperties = await _ctx.Properties.CountDocumentsAsync(filter, cancellationToken: ct);
        var totalOwners = await _ctx.Owners.CountDocumentsAsync(x => !x.IsDeleted, cancellationToken: ct);
        
        // No Enabled field; compute active/pending as placeholders using CreatedAt windows
        var last30 = DateTime.UtcNow.AddDays(-30);
        var activeProperties = await _ctx.Properties.CountDocumentsAsync(
            filter & Builders<PropertyDocument>.Filter.Gte(x => x.CreatedAt, last30), cancellationToken: ct);
        
        var pendingProperties = (int)(totalProperties - activeProperties);

        // Revenue calculations
        var totalRevenue = await CalculateTotalRevenueAsync(filter, ct);
        var monthlyRevenue = await CalculateMonthlyRevenueAsync(ct);
        var yearlyRevenue = await CalculateYearlyRevenueAsync(ct);

        // Chart data
        var propertiesByMonth = await GetPropertiesByMonthAsync(filter, ct);
        var revenueByMonth = await GetRevenueByMonthAsync(ct);
        var propertiesByOperationType = await GetPropertiesByOperationTypeAsync(filter, ct);

        return new DashboardAnalyticsDto
        {
            TotalProperties = (int)totalProperties,
            TotalOwners = (int)totalOwners,
            ActiveProperties = (int)activeProperties,
            PendingProperties = (int)pendingProperties,
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            YearlyRevenue = yearlyRevenue,
            PropertiesByMonth = propertiesByMonth,
            RevenueByMonth = revenueByMonth,
            PropertiesByOperationType = propertiesByOperationType
        };
    }

    public async Task<PropertyAnalyticsDto> GetPropertyAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, string? operationType = null, CancellationToken ct = default)
    {
        var filter = Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false);
        
        if (startDate.HasValue && endDate.HasValue)
        {
            filter &= Builders<PropertyDocument>.Filter.Gte(x => x.CreatedAt, startDate.Value) &
                     Builders<PropertyDocument>.Filter.Lte(x => x.CreatedAt, endDate.Value);
        }

        if (!string.IsNullOrEmpty(operationType))
        {
            filter &= Builders<PropertyDocument>.Filter.Eq(x => x.OperationType, operationType);
        }

        var totalProperties = await _ctx.Properties.CountDocumentsAsync(filter, cancellationToken: ct);
        var saleProperties = await _ctx.Properties.CountDocumentsAsync(
            filter & Builders<PropertyDocument>.Filter.Eq(x => x.OperationType, "sale"), cancellationToken: ct);
        var rentProperties = await _ctx.Properties.CountDocumentsAsync(
            filter & Builders<PropertyDocument>.Filter.Eq(x => x.OperationType, "rent"), cancellationToken: ct);

        // Price calculations
        var averagePrice = await CalculateAveragePriceAsync(filter, ct);
        var averageRentPrice = await CalculateAveragePriceAsync(
            filter & Builders<PropertyDocument>.Filter.Eq(x => x.OperationType, "rent"), ct);
        var averageSalePrice = await CalculateAveragePriceAsync(
            filter & Builders<PropertyDocument>.Filter.Eq(x => x.OperationType, "sale"), ct);

        // Chart data
        var propertiesByLocation = await GetPropertiesByLocationAsync(filter, ct);
        var propertiesByPriceRange = await GetPropertiesByPriceRangeAsync(filter, ct);
        var propertiesByBedrooms = await GetPropertiesByBedroomsAsync(filter, ct);
        var propertiesByBathrooms = await GetPropertiesByBathroomsAsync(filter, ct);

        return new PropertyAnalyticsDto
        {
            TotalProperties = (int)totalProperties,
            SaleProperties = (int)saleProperties,
            RentProperties = (int)rentProperties,
            AveragePrice = averagePrice,
            AverageRentPrice = averageRentPrice,
            AverageSalePrice = averageSalePrice,
            PropertiesByLocation = propertiesByLocation,
            PropertiesByPriceRange = propertiesByPriceRange,
            PropertiesByBedrooms = propertiesByBedrooms,
            PropertiesByBathrooms = propertiesByBathrooms
        };
    }

    public async Task<OwnerAnalyticsDto> GetOwnerAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        var filter = Builders<OwnerDocument>.Filter.Eq(x => x.IsDeleted, false);
        
        if (startDate.HasValue && endDate.HasValue)
        {
            filter &= Builders<OwnerDocument>.Filter.Gte(x => x.CreatedAt, startDate.Value) &
                     Builders<OwnerDocument>.Filter.Lte(x => x.CreatedAt, endDate.Value);
        }

        var totalOwners = await _ctx.Owners.CountDocumentsAsync(filter, cancellationToken: ct);
        var activeOwners = await _ctx.Owners.CountDocumentsAsync(filter, cancellationToken: ct);
        
        var newOwnersThisMonth = await _ctx.Owners.CountDocumentsAsync(
            filter & Builders<OwnerDocument>.Filter.Gte(x => x.CreatedAt, DateTime.UtcNow.AddMonths(-1)), cancellationToken: ct);

        var averagePropertiesPerOwner = await CalculateAveragePropertiesPerOwnerAsync(ct);

        // Chart data
        var ownersByMonth = await GetOwnersByMonthAsync(filter, ct);
        var topOwnersByProperties = await GetTopOwnersByPropertiesAsync(ct);

        return new OwnerAnalyticsDto
        {
            TotalOwners = (int)totalOwners,
            ActiveOwners = (int)activeOwners,
            NewOwnersThisMonth = (int)newOwnersThisMonth,
            AveragePropertiesPerOwner = averagePropertiesPerOwner,
            OwnersByMonth = ownersByMonth,
            TopOwnersByProperties = topOwnersByProperties
        };
    }

    public async Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate, string groupBy = "month", CancellationToken ct = default)
    {
        var filter = Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false) &
                    Builders<PropertyDocument>.Filter.Gte(x => x.CreatedAt, startDate) &
                    Builders<PropertyDocument>.Filter.Lte(x => x.CreatedAt, endDate);

        var totalRevenue = await CalculateTotalRevenueAsync(filter, ct);
        var averageRevenue = await CalculateAverageRevenueAsync(filter, ct);
        var revenueGrowth = await CalculateRevenueGrowthAsync(startDate, endDate, ct);

        // Chart data
        var revenueByPeriod = await GetRevenueByPeriodAsync(startDate, endDate, groupBy, ct);
        var revenueByOperationType = await GetRevenueByOperationTypeAsync(filter, ct);
        var revenueByLocation = await GetRevenueByLocationAsync(filter, ct);

        return new RevenueAnalyticsDto
        {
            TotalRevenue = totalRevenue,
            AverageRevenue = averageRevenue,
            RevenueGrowth = revenueGrowth,
            RevenueByPeriod = revenueByPeriod,
            RevenueByOperationType = revenueByOperationType,
            RevenueByLocation = revenueByLocation
        };
    }

    public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        // This would typically integrate with application performance monitoring
        // For now, returning mock data structure
        return new PerformanceMetricsDto
        {
            AverageResponseTime = 150.5,
            TotalRequests = 10000,
            SuccessfulRequests = 9850,
            FailedRequests = 150,
            SuccessRate = 98.5,
            ResponseTimeByHour = new List<ChartDataPoint>(),
            RequestsByEndpoint = new List<ChartDataPoint>()
        };
    }

    #region Private Methods

    private async Task<decimal> CalculateTotalRevenueAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var result = await _ctx.Properties
            .Aggregate()
            .Match(filter)
            .Group(BsonNull.Value, g => new { totalRevenue = g.Sum(x => x.Price) })
            .FirstOrDefaultAsync(ct);
        return result?.totalRevenue ?? 0;
    }

    private async Task<decimal> CalculateMonthlyRevenueAsync(CancellationToken ct)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var filter = Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false) &
                    Builders<PropertyDocument>.Filter.Gte(x => x.CreatedAt, startOfMonth);
        
        return await CalculateTotalRevenueAsync(filter, ct);
    }

    private async Task<decimal> CalculateYearlyRevenueAsync(CancellationToken ct)
    {
        var startOfYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
        var filter = Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false) &
                    Builders<PropertyDocument>.Filter.Gte(x => x.CreatedAt, startOfYear);
        
        return await CalculateTotalRevenueAsync(filter, ct);
    }

    private async Task<decimal> CalculateAveragePriceAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var result = await _ctx.Properties
            .Aggregate()
            .Match(filter)
            .Group(BsonNull.Value, g => new { averagePrice = g.Average(x => x.Price) })
            .FirstOrDefaultAsync(ct);
        return result?.averagePrice ?? 0;
    }

    private async Task<decimal> CalculateAverageRevenueAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        return await CalculateAveragePriceAsync(filter, ct);
    }

    private async Task<decimal> CalculateRevenueGrowthAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        var previousStartDate = startDate.AddMonths(-1);
        var previousEndDate = endDate.AddMonths(-1);

        var currentRevenue = await CalculateTotalRevenueAsync(
            Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false) &
            Builders<PropertyDocument>.Filter.Gte(x => x.CreatedAt, startDate) &
            Builders<PropertyDocument>.Filter.Lte(x => x.CreatedAt, endDate), ct);

        var previousRevenue = await CalculateTotalRevenueAsync(
            Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false) &
            Builders<PropertyDocument>.Filter.Gte(x => x.CreatedAt, previousStartDate) &
            Builders<PropertyDocument>.Filter.Lte(x => x.CreatedAt, previousEndDate), ct);

        if (previousRevenue == 0) return 0;
        return ((currentRevenue - previousRevenue) / previousRevenue) * 100;
    }

    private async Task<decimal> CalculateAveragePropertiesPerOwnerAsync(CancellationToken ct)
    {
        var totalProperties = await _ctx.Properties.CountDocumentsAsync(x => !x.IsDeleted, cancellationToken: ct);
        var totalOwners = await _ctx.Owners.CountDocumentsAsync(x => !x.IsDeleted, cancellationToken: ct);
        
        if (totalOwners == 0) return 0;
        return (decimal)totalProperties / (decimal)totalOwners;
    }

    private async Task<List<ChartDataPoint>> GetPropertiesByMonthAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.ToBsonDocument()),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "year", new BsonDocument("$year", "$CreatedAt") },
                        { "month", new BsonDocument("$month", "$CreatedAt") }
                    }
                },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = $"{doc["_id"]["month"]}/{doc["_id"]["year"]}",
            Count = doc["count"].AsInt32,
            Value = doc["count"].AsInt32
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetRevenueByMonthAsync(CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("IsDeleted", false)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "year", new BsonDocument("$year", "$CreatedAt") },
                        { "month", new BsonDocument("$month", "$CreatedAt") }
                    }
                },
                { "revenue", new BsonDocument("$sum", "$Price") }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = $"{doc["_id"]["month"]}/{doc["_id"]["year"]}",
            Value = doc["revenue"].AsDecimal
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetPropertiesByOperationTypeAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.Render(_ctx.Properties.DocumentSerializer, _ctx.Properties.Settings.SerializerRegistry)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$OperationType" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = doc["_id"].AsString,
            Count = doc["count"].AsInt32,
            Value = doc["count"].AsInt32
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetPropertiesByLocationAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.Render(_ctx.Properties.DocumentSerializer, _ctx.Properties.Settings.SerializerRegistry)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Address.City" },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("count", -1)),
            new BsonDocument("$limit", 10)
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = doc["_id"].AsString ?? "Unknown",
            Count = doc["count"].AsInt32,
            Value = doc["count"].AsInt32
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetPropertiesByPriceRangeAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.Render(_ctx.Properties.DocumentSerializer, _ctx.Properties.Settings.SerializerRegistry)),
            new BsonDocument("$bucket", new BsonDocument
            {
                { "groupBy", "$Price" },
                { "boundaries", new BsonArray { 0, 100000, 250000, 500000, 1000000, 2500000, 5000000, 10000000 } },
                { "default", "10M+" },
                { "output", new BsonDocument
                    {
                        { "count", new BsonDocument("$sum", 1) }
                    }
                }
            })
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = doc["_id"].AsString ?? "Unknown",
            Count = doc["count"].AsInt32,
            Value = doc["count"].AsInt32
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetPropertiesByBedroomsAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.Render(_ctx.Properties.DocumentSerializer, _ctx.Properties.Settings.SerializerRegistry)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Beds" },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = doc["_id"].AsInt32.ToString(),
            Count = doc["count"].AsInt32,
            Value = doc["count"].AsInt32
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetPropertiesByBathroomsAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.Render(_ctx.Properties.DocumentSerializer, _ctx.Properties.Settings.SerializerRegistry)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Baths" },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = doc["_id"].AsDouble.ToString("F1"),
            Count = doc["count"].AsInt32,
            Value = doc["count"].AsInt32
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetOwnersByMonthAsync(FilterDefinition<OwnerDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.Render(_ctx.Owners.DocumentSerializer, _ctx.Owners.Settings.SerializerRegistry)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "year", new BsonDocument("$year", "$CreatedAt") },
                        { "month", new BsonDocument("$month", "$CreatedAt") }
                    }
                },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var result = await _ctx.Owners.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = $"{doc["_id"]["month"]}/{doc["_id"]["year"]}",
            Count = doc["count"].AsInt32,
            Value = doc["count"].AsInt32
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetTopOwnersByPropertiesAsync(CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("IsDeleted", false)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$OwnerId" },
                { "propertyCount", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("propertyCount", -1)),
            new BsonDocument("$limit", 10)
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = doc["_id"].AsString,
            Count = doc["propertyCount"].AsInt32,
            Value = doc["propertyCount"].AsInt32
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate, string groupBy, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "IsDeleted", false },
                { "CreatedAt", new BsonDocument
                    {
                        { "$gte", startDate },
                        { "$lte", endDate }
                    }
                }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", groupBy == "month" ? new BsonDocument
                    {
                        { "year", new BsonDocument("$year", "$CreatedAt") },
                        { "month", new BsonDocument("$month", "$CreatedAt") }
                    } : new BsonDocument("$dateToString", new BsonDocument
                    {
                        { "format", "%Y-%m-%d" },
                        { "date", "$CreatedAt" }
                    }) as BsonValue
                },
                { "revenue", new BsonDocument("$sum", "$Price") }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = groupBy == "month" ? $"{doc["_id"]["month"]}/{doc["_id"]["year"]}" : doc["_id"].AsString,
            Value = doc["revenue"].AsDecimal
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetRevenueByOperationTypeAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.Render(_ctx.Properties.DocumentSerializer, _ctx.Properties.Settings.SerializerRegistry)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$OperationType" },
                { "revenue", new BsonDocument("$sum", "$Price") }
            })
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = doc["_id"].AsString,
            Value = doc["revenue"].AsDecimal
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetRevenueByLocationAsync(FilterDefinition<PropertyDocument> filter, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.Render(_ctx.Properties.DocumentSerializer, _ctx.Properties.Settings.SerializerRegistry)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Address.City" },
                { "revenue", new BsonDocument("$sum", "$Price") }
            }),
            new BsonDocument("$sort", new BsonDocument("revenue", -1)),
            new BsonDocument("$limit", 10)
        };

        var result = await _ctx.Properties.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
        var data = await result.ToListAsync(ct);

        return data.Select(doc => new ChartDataPoint
        {
            Label = doc["_id"].AsString ?? "Unknown",
            Value = doc["revenue"].AsDecimal
        }).ToList();
    }

    #endregion
}
