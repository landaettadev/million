using System.ComponentModel.DataAnnotations;

namespace RealEstate.Application;

// DTOs for writes (only unique ones not in Abstractions.cs)
public sealed record AddImageDto(
    string PropertyId,
    string File,
    bool Enabled,
    int Order,
    long FileSize = 0,
    string ContentType = "image/jpeg",
    string? ThumbnailFile = null
);

// Write service contracts
public interface IPropertyWriteService
{
    Task<string> CreateAsync(CreatePropertyDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(string id, UpdatePropertyDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<bool> UndeleteAsync(string id, CancellationToken ct = default);
    Task<bool> SetFeaturedAsync(string id, bool isFeatured, CancellationToken ct = default);
}

public interface IOwnerWriteService
{
    Task<string> CreateAsync(CreateOwnerDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(string id, UpdateOwnerDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<bool> UndeleteAsync(string id, CancellationToken ct = default);
}

public interface IImageWriteService
{
    Task<string> AddAsync(AddImageDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}


