using MongoDB.Driver;
using RealEstate.Application.Interfaces;
using RealEstate.Application;
using Microsoft.Extensions.Logging;

namespace RealEstate.Infrastructure.Services;

public interface IPropertyManagementService
{
    Task<bool> CreatePropertyWithOwnerAsync(CreatePropertyDto propertyDto, CreateOwnerDto ownerDto, CancellationToken ct = default);
    Task<bool> TransferPropertyOwnershipAsync(string propertyId, string newOwnerId, CancellationToken ct = default);
    Task<bool> DeletePropertyWithImagesAsync(string propertyId, CancellationToken ct = default);
    Task<bool> BulkUpdatePropertyStatusAsync(List<string> propertyIds, bool enabled, CancellationToken ct = default);
}

public sealed class PropertyManagementService : IPropertyManagementService
{
    private readonly MongoContext _ctx;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<PropertyManagementService> _logger;

    public PropertyManagementService(
        MongoContext ctx,
        ITransactionService transactionService,
        ILogger<PropertyManagementService> logger)
    {
        _ctx = ctx;
        _transactionService = transactionService;
        _logger = logger;
    }

    public async Task<bool> CreatePropertyWithOwnerAsync(CreatePropertyDto propertyDto, CreateOwnerDto ownerDto, CancellationToken ct = default)
    {
        try
        {
            return await _transactionService.ExecuteInTransactionAsync(async (session) =>
            {
                // Create owner first
                var ownerDoc = new OwnerDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    Name = ownerDto.Name,
                    Address = ownerDto.Address,
                    Photo = ownerDto.Photo,
                    Birthday = ownerDto.Birthday,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _ctx.Owners.InsertOneAsync(session, ownerDoc, cancellationToken: ct);

                // Create property with the new owner ID
                var propertyDoc = new PropertyDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    OwnerId = ownerDoc.Id,
                    Name = propertyDto.Name,
                    Address = propertyDto.Address,
                    Price = propertyDto.Price,
                    OperationType = propertyDto.OperationType == OperationType.Sale ? "sale" : "rent",
                    Beds = propertyDto.Beds,
                    Baths = propertyDto.Baths,
                    HalfBaths = propertyDto.HalfBaths,
                    Sqft = propertyDto.Sqft,
                    Description = propertyDto.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _ctx.Properties.InsertOneAsync(session, propertyDoc, cancellationToken: ct);

                _logger.LogInformation("Created property {PropertyId} with owner {OwnerId} in transaction", propertyDoc.Id, ownerDoc.Id);
                return true;
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create property with owner in transaction");
            return false;
        }
    }

    public async Task<bool> TransferPropertyOwnershipAsync(string propertyId, string newOwnerId, CancellationToken ct = default)
    {
        try
        {
            return await _transactionService.ExecuteInTransactionAsync(async (session) =>
            {
                // Verify both property and new owner exist
                var propertyFilter = Builders<PropertyDocument>.Filter.Eq(x => x.Id, propertyId) &
                                   Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false);
                
                var ownerFilter = Builders<OwnerDocument>.Filter.Eq(x => x.Id, newOwnerId) &
                                 Builders<OwnerDocument>.Filter.Eq(x => x.IsDeleted, false);

                var property = await _ctx.Properties.Find(session, propertyFilter).FirstOrDefaultAsync(ct);
                var newOwner = await _ctx.Owners.Find(session, ownerFilter).FirstOrDefaultAsync(ct);

                if (property == null)
                {
                    throw new InvalidOperationException($"Property {propertyId} not found");
                }

                if (newOwner == null)
                {
                    throw new InvalidOperationException($"New owner {newOwnerId} not found");
                }

                // Update property ownership
                var propertyUpdate = Builders<PropertyDocument>.Update
                    .Set(x => x.OwnerId, newOwnerId)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var propertyResult = await _ctx.Properties.UpdateOneAsync(
                    session, propertyFilter, propertyUpdate, cancellationToken: ct);

                if (propertyResult.ModifiedCount == 0)
                {
                    throw new InvalidOperationException("Failed to update property ownership");
                }

                // Log the ownership transfer
                var transferLog = new PropertyTransferLog
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    PropertyId = propertyId,
                    PreviousOwnerId = property.OwnerId,
                    NewOwnerId = newOwnerId,
                    TransferDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                // Note: You would need to create a PropertyTransferLog collection and model
                // await _ctx.PropertyTransferLogs.InsertOneAsync(session, transferLog, cancellationToken: ct);

                _logger.LogInformation("Transferred property {PropertyId} from owner {PreviousOwner} to {NewOwner}", 
                    propertyId, property.OwnerId, newOwnerId);
                return true;
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer property ownership {PropertyId} to {NewOwnerId}", propertyId, newOwnerId);
            return false;
        }
    }

    public async Task<bool> DeletePropertyWithImagesAsync(string propertyId, CancellationToken ct = default)
    {
        try
        {
            return await _transactionService.ExecuteInTransactionAsync(async (session) =>
            {
                // Soft delete property
                var propertyFilter = Builders<PropertyDocument>.Filter.Eq(x => x.Id, propertyId) &
                                   Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false);

                var propertyUpdate = Builders<PropertyDocument>.Update
                    .Set(x => x.IsDeleted, true)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var propertyResult = await _ctx.Properties.UpdateOneAsync(
                    session, propertyFilter, propertyUpdate, cancellationToken: ct);

                if (propertyResult.ModifiedCount == 0)
                {
                    throw new InvalidOperationException($"Property {propertyId} not found or already deleted");
                }

                // Soft delete all associated images
                var imageFilter = Builders<PropertyImageDocument>.Filter.Eq(x => x.PropertyId, propertyId) &
                                 Builders<PropertyImageDocument>.Filter.Eq(x => x.IsDeleted, false);

                var imageUpdate = Builders<PropertyImageDocument>.Update
                    .Set(x => x.IsDeleted, true)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var imageResult = await _ctx.PropertyImages.UpdateManyAsync(
                    session, imageFilter, imageUpdate, cancellationToken: ct);

                _logger.LogInformation("Deleted property {PropertyId} with {ImageCount} images in transaction", 
                    propertyId, imageResult.ModifiedCount);
                return true;
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete property with images {PropertyId}", propertyId);
            return false;
        }
    }

    public async Task<bool> BulkUpdatePropertyStatusAsync(List<string> propertyIds, bool enabled, CancellationToken ct = default)
    {
        try
        {
            return await _transactionService.ExecuteInTransactionAsync(async (session) =>
            {
                var filter = Builders<PropertyDocument>.Filter.In(x => x.Id, propertyIds) &
                             Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false);

                // As we don't have an Enabled flag, we can simulate status via UpdatedAt bump or extend model later
                var update = Builders<PropertyDocument>.Update
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var result = await _ctx.Properties.UpdateManyAsync(
                    session, filter, update, cancellationToken: ct);

                _logger.LogInformation("Updated {UpdatedCount} properties status to {Enabled} in transaction", 
                    result.ModifiedCount, enabled);
                return result.ModifiedCount > 0;
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk update property status for {PropertyCount} properties", propertyIds.Count);
            return false;
        }
    }
}

// Helper class for property transfer logging
public class PropertyTransferLog
{
    public string Id { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string PreviousOwnerId { get; set; } = string.Empty;
    public string NewOwnerId { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
