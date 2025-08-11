namespace RealEstate.Application.DTOs;

public class AdminUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

public class PropertyImageDto
{
    public string Id { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string? ThumbnailFile { get; set; }
    public bool Enabled { get; set; }
    public int Order { get; set; } = 1;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class UpdatePropertyImageDto
{
    public bool? Enabled { get; set; }
    public int? Order { get; set; }
}

public class OwnerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Photo { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOwnerDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
}

public class UpdateOwnerDto
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public DateTime? Birthday { get; set; }
}

public class AdminPropertyDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Beds { get; set; }
    public int? Baths { get; set; }
    public int? HalfBaths { get; set; }
    public int? Sqft { get; set; }
    public string? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
}

public class CreatePropertyDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Beds { get; set; }
    public int? Baths { get; set; }
    public int? HalfBaths { get; set; }
    public int? Sqft { get; set; }
    public string? OwnerId { get; set; }
}

public class UpdatePropertyDto
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public decimal? Price { get; set; }
    public string? OperationType { get; set; }
    public string? Description { get; set; }
    public int? Beds { get; set; }
    public int? Baths { get; set; }
    public int? HalfBaths { get; set; }
    public int? Sqft { get; set; }
    public string? OwnerId { get; set; }
    public bool? IsFeatured { get; set; }
}

public class AnalyticsDto
{
    public int TotalProperties { get; set; }
    public int TotalOwners { get; set; }
    public int TotalImages { get; set; }
    public decimal TotalValue { get; set; }
    public List<MonthlyStats> MonthlyStats { get; set; } = new();
}

public class MonthlyStats
{
    public string Month { get; set; } = string.Empty;
    public int PropertiesAdded { get; set; }
    public decimal TotalValue { get; set; }
}
