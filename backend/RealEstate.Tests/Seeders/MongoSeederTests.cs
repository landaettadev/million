using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RealEstate.Infrastructure;
using Testcontainers.MongoDb;
using Xunit;

namespace RealEstate.Tests.Seeders;

public class MongoSeederTests : IAsyncDisposable
{
    private readonly MongoDbContainer _mongoContainer;
    private readonly IMongoDatabase _database;
    private readonly MongoSeeder _seeder;

    public MongoSeederTests()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();

        _mongoContainer.StartAsync().Wait();

        var connectionString = _mongoContainer.GetConnectionString();
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("testdb");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:Enabled"] = "true",
                ["Seed:InsertIfEmpty"] = "true",
                ["Seed:Count"] = "5"
            })
            .Build();

        var mongoSettings = new MongoSettings
        {
            ConnectionString = connectionString,
            Database = "testdb",
            CollectionNames = new CollectionNames
            {
                Properties = "properties",
                PropertyImages = "propertyImages",
                PropertyTraces = "propertyTraces",
                Owners = "owners"
            }
        };

        _seeder = new MongoSeeder(_database, mongoSettings, configuration);
    }

    [Fact]
    public async Task RunAsync_WithEmptyDatabase_ShouldCreateIndexesAndSeedData()
    {
        // Act
        await _seeder.RunAsync();

        // Assert
        var propertiesCollection = _database.GetCollection<dynamic>("properties");
        var imagesCollection = _database.GetCollection<dynamic>("propertyImages");

        // Check that data was seeded
        var propertiesCount = await propertiesCollection.CountDocumentsAsync("{}");
        var imagesCount = await imagesCollection.CountDocumentsAsync("{}");

        propertiesCount.Should().BeGreaterThan(0);
        imagesCount.Should().BeGreaterThan(0);

        // Check that indexes were created
        var indexes = await propertiesCollection.Indexes.ListAsync();
        var indexList = await indexes.ToListAsync();
        
        indexList.Should().Contain(i => i["name"].AsString == "price_1");
        indexList.Should().Contain(i => i["name"].AsString == "name_text_address_text");
    }

    [Fact]
    public async Task RunAsync_WithExistingData_ShouldNotSeedAgain()
    {
        // Arrange - Insert some existing data
        var propertiesCollection = _database.GetCollection<dynamic>("properties");
        await propertiesCollection.InsertOneAsync(new
        {
            _id = "existing1",
            ownerId = "owner1",
            name = "Existing Property",
            address = "123 Existing St",
            price = 1000000,
            operationType = "Sale"
        });

        // Act
        await _seeder.RunAsync();

        // Assert - Should not add more data since collection is not empty
        var propertiesCount = await propertiesCollection.CountDocumentsAsync("{}");
        propertiesCount.Should().Be(1); // Only the existing one
    }

    [Fact]
    public async Task RunAsync_WithDisabledSeeding_ShouldNotSeedData()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:Enabled"] = "false",
                ["Seed:InsertIfEmpty"] = "true",
                ["Seed:Count"] = "5"
            })
            .Build();

        var seeder = new MongoSeeder(_database, new MongoSettings(), configuration);

        // Act
        await seeder.RunAsync();

        // Assert
        var propertiesCollection = _database.GetCollection<dynamic>("properties");
        var propertiesCount = await propertiesCollection.CountDocumentsAsync("{}");
        propertiesCount.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithCustomSeedCount_ShouldSeedCorrectAmount()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:Enabled"] = "true",
                ["Seed:InsertIfEmpty"] = "true",
                ["Seed:Count"] = "3"
            })
            .Build();

        var seeder = new MongoSeeder(_database, new MongoSettings(), configuration);

        // Act
        await seeder.RunAsync();

        // Assert
        var propertiesCollection = _database.GetCollection<dynamic>("properties");
        var propertiesCount = await propertiesCollection.CountDocumentsAsync("{}");
        propertiesCount.Should().Be(3);
    }

    [Fact]
    public async Task RunAsync_ShouldCreateProperIndexes()
    {
        // Act
        await _seeder.RunAsync();

        // Assert
        var propertiesCollection = _database.GetCollection<dynamic>("properties");
        var imagesCollection = _database.GetCollection<dynamic>("propertyImages");

        // Check properties indexes
        var propertiesIndexes = await propertiesCollection.Indexes.ListAsync();
        var propertiesIndexList = await propertiesIndexes.ToListAsync();
        
        propertiesIndexList.Should().Contain(i => i["name"].AsString == "Price_1");
        propertiesIndexList.Should().Contain(i => i["name"].AsString == "search_text");
        // Check images indexes
        var imagesIndexes = await imagesCollection.Indexes.ListAsync();
        var imagesIndexList = await imagesIndexes.ToListAsync();
        
        imagesIndexList.Should().Contain(i => i["name"].AsString.Contains("PropertyId_1_Enabled_1"));
    }

    public async ValueTask DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }
}
