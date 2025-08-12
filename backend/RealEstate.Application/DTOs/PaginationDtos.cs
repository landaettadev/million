using System.ComponentModel.DataAnnotations;

namespace RealEstate.Application.DTOs;

public class PaginationRequest
{
    [Range(1, 1000, ErrorMessage = "Page must be between 1 and 1000")]
    public int Page { get; set; } = 1;
    
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
    
    public string? SortBy { get; set; }
    
    public string SortDirection { get; set; } = "asc"; // asc, desc
    
    public int Skip => (Page - 1) * PageSize;
    
    public int Take => PageSize;
}

public class PaginationResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public int NextPage { get; set; }
    public int PreviousPage { get; set; }
    
    public PaginationResponse(List<T> data, int totalCount, PaginationRequest request)
    {
        Data = data;
        TotalCount = totalCount;
        Page = request.Page;
        PageSize = request.PageSize;
        TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        HasNextPage = Page < TotalPages;
        HasPreviousPage = Page > 1;
        NextPage = HasNextPage ? Page + 1 : Page;
        PreviousPage = HasPreviousPage ? Page - 1 : Page;
    }
}

public class PropertySearchRequest : PaginationRequest
{
    // Basic filters
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? OperationType { get; set; } // sale, rent
    
    // Price range
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    
    // Property characteristics
    public int? MinBeds { get; set; }
    public int? MaxBeds { get; set; }
    public int? MinBaths { get; set; }
    public int? MaxBaths { get; set; }
    public int? MinSqft { get; set; }
    public int? MaxSqft { get; set; }
    
    // Year range
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    
    // Location filters
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    
    // Advanced filters
    public bool? HasPool { get; set; }
    public bool? HasGarden { get; set; }
    public bool? HasParking { get; set; }
    public bool? IsFurnished { get; set; }
    
    // Date filters
    public DateTime? ListedAfter { get; set; }
    public DateTime? ListedBefore { get; set; }
    
    // Search options
    public bool UseFullTextSearch { get; set; } = true;
    public bool IncludeInactive { get; set; } = false;
    
    // Validation
    public bool IsValid()
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            return false;
            
        if (MinBeds.HasValue && MaxBeds.HasValue && MinBeds > MaxBeds)
            return false;
            
        if (MinBaths.HasValue && MaxBaths.HasValue && MinBaths > MaxBaths)
            return false;
            
        if (MinSqft.HasValue && MaxSqft.HasValue && MinSqft > MaxSqft)
            return false;
            
        if (MinYear.HasValue && MaxYear.HasValue && MinYear > MaxYear)
            return false;
            
        return true;
    }
    
    public string GetCacheKey()
    {
        var filters = new[]
        {
            Name,
            Address,
            OperationType,
            MinPrice?.ToString(),
            MaxPrice?.ToString(),
            MinBeds?.ToString(),
            MaxBeds?.ToString(),
            MinBaths?.ToString(),
            MaxBaths?.ToString(),
            MinSqft?.ToString(),
            MaxSqft?.ToString(),
            MinYear?.ToString(),
            MaxYear?.ToString(),
            City,
            State,
            ZipCode,
            HasPool?.ToString(),
            HasGarden?.ToString(),
            HasParking?.ToString(),
            IsFurnished?.ToString(),
            ListedAfter?.ToString("yyyyMMdd"),
            ListedBefore?.ToString("yyyyMMdd"),
            UseFullTextSearch.ToString(),
            IncludeInactive.ToString(),
            Page.ToString(),
            PageSize.ToString(),
            SortBy,
            SortDirection
        };
        
        return $"property_search:{string.Join("|", filters.Where(f => !string.IsNullOrEmpty(f)))}";
    }
}

public class PropertySearchResponse
{
    public List<PropertyLiteDto> Properties { get; set; } = new();
    public PaginationResponse<PropertyLiteDto> Pagination { get; set; } = null!;
    public PropertySearchStats Stats { get; set; } = new();
    public List<PropertySearchFacet> Facets { get; set; } = new();
    public string SearchId { get; set; } = Guid.NewGuid().ToString();
    public DateTime SearchTimestamp { get; set; } = DateTime.UtcNow;
}

public class PropertySearchStats
{
    public int TotalProperties { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalBeds { get; set; }
    public int TotalBaths { get; set; }
    public int TotalSqft { get; set; }
    public Dictionary<string, int> OperationTypeDistribution { get; set; } = new();
    public Dictionary<string, int> CityDistribution { get; set; } = new();
}

public class PropertySearchFacet
{
    public string Field { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<FacetValue> Values { get; set; } = new();
}

public class FacetValue
{
    public string Value { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class PropertySortOptions
{
    public const string PriceAsc = "price_asc";
    public const string PriceDesc = "price_desc";
    public const string NameAsc = "name_asc";
    public const string NameDesc = "name_desc";
    public const string DateListedDesc = "date_listed_desc";
    public const string DateListedAsc = "date_listed_asc";
    public const string BedsDesc = "beds_desc";
    public const string SqftDesc = "sqft_desc";
    public const string Relevance = "relevance";
    
    public static readonly Dictionary<string, string> DisplayNames = new()
    {
        [PriceAsc] = "Price: Low to High",
        [PriceDesc] = "Price: High to Low",
        [NameAsc] = "Name: A to Z",
        [NameDesc] = "Name: Z to A",
        [DateListedDesc] = "Newest First",
        [DateListedAsc] = "Oldest First",
        [BedsDesc] = "Most Bedrooms",
        [SqftDesc] = "Largest Square Footage",
        [Relevance] = "Most Relevant"
    };
    
    public static bool IsValid(string sortBy)
    {
        return DisplayNames.ContainsKey(sortBy);
    }
}
