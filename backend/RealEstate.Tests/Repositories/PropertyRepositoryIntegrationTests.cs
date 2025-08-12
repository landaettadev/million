using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;
using Testcontainers.MongoDb;
using NSubstitute;

namespace RealEstate.Tests.Repositories;

public class PropertyRepositoryIntegrationTests : IAsyncDisposable
{
    private readonly MongoDbContainer _mongoContainer;
    private readonly IMongoDatabase _database;
    private readonly PropertyRepository _repository;

    public PropertyRepositoryIntegrationTests()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();

        _mongoContainer.StartAsync().Wait();

        var connectionString = _mongoContainer.GetConnectionString();
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("testdb");

        var context = new MongoContext(_database);
        var mockImageStorageService = Substitute.For<IImageStorageService>();
        mockImageStorageService.GetImageUrl(Arg.Any<string>()).Returns(callInfo =>
        {
            var file = callInfo.ArgAt<string>(0);
            return $"https://example.com/{Uri.EscapeDataString(file)}";
        });
        _repository = new PropertyRepository(context, mockImageStorageService);
    }

    [Test]
    public async Task SearchAsync_WithNoFilters_ShouldReturnAllProperties()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery());

        // Assert
        result.Items.Should().HaveCount(3);
        result.Total.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Test]
    public async Task SearchAsync_WithNameFilter_ShouldReturnMatchingProperties()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery(Name: "Luxury"));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Name.Contains("Luxury", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task SearchAsync_WithAddressFilter_ShouldReturnMatchingProperties()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery(Address: "Park"));

        // Assert
        // Only one address contains "Park"
        result.Items.Should().HaveCount(1);
        result.Items.Should().OnlyContain(p => p.Address.Contains("Park", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task SearchAsync_WithPriceRange_ShouldReturnPropertiesInRange()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery(MinPrice: 2000000, MaxPrice: 4000000));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Price >= 2000000 && p.Price <= 4000000);
    }

    [Test]
    public async Task SearchAsync_WithOperationTypeFilter_ShouldReturnMatchingProperties()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery(OperationType: Application.OperationType.Sale));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.OperationType == Application.OperationType.Sale);
    }

    [Test]
    public async Task SearchAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery(Page: 2, PageSize: 2));

        // Assert
        result.Items.Should().HaveCount(1);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Total.Should().Be(3);
    }

    [Test]
    public async Task SearchAsync_WithMultipleFilters_ShouldReturnMatchingProperties()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery(
            Name: "Luxury",
            MinPrice: 2000000,
            OperationType: Application.OperationType.Sale
        ));

        // Assert
        // Two properties match name contains "Luxury" and Sale, but with MinPrice >= 2,000,000 both still match
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => 
            p.Name.Contains("Luxury", StringComparison.OrdinalIgnoreCase) &&
            p.Price >= 2000000 &&
            p.OperationType == Application.OperationType.Sale
        );
    }

    [Test]
    public async Task GetByIdAsync_WithValidId_ShouldReturnPropertyWithImages()
    {
        // Arrange
        await SeedTestData();
        
        // Get the first property ID from the database
        var propertiesCollection = _database.GetCollection<PropertyDocument>("properties");
        var firstProperty = await propertiesCollection.Find(Builders<PropertyDocument>.Filter.Empty).FirstAsync();
        var propertyId = firstProperty.Id;

        // Act
        var result = await _repository.GetByIdAsync(propertyId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(propertyId);
        result.Images.Should().HaveCount(2);
        result.Images.Should().Contain("image1.jpg");
        result.Images.Should().Contain("image2.jpg");
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.GetByIdAsync(MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task SearchAsync_ShouldReturnOnlyFirstEnabledImage()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery());

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(p => p.Image != null && (
            p.Image == "https://example.com/image1.jpg" ||
            p.Image == "https://example.com/image4.jpg" ||
            p.Image == "https://example.com/image6.jpg"
        ));
    }

    [Test]
    public async Task SearchAsync_ShouldOrderByPriceAscending()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _repository.SearchAsync(new SearchPropertiesQuery());

        // Assert
        result.Items.Should().BeInAscendingOrder(p => p.Price);
    }

    private async Task SeedTestData()
    {
        var propertiesCollection = _database.GetCollection<PropertyDocument>("properties");
        var imagesCollection = _database.GetCollection<PropertyImageDocument>("propertyImages");

        // Clear existing data
        await propertiesCollection.DeleteManyAsync(Builders<PropertyDocument>.Filter.Empty);
        await imagesCollection.DeleteManyAsync(Builders<PropertyImageDocument>.Filter.Empty);

        // Insert test properties
        var properties = new List<PropertyDocument>
        {
            new()
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                OwnerId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = "Luxury Apartment",
                Address = "123 Park Avenue",
                Price = 2500000,
                OperationType = "sale",
                Beds = 3,
                Baths = 2,
                HalfBaths = 1,
                Sqft = 2500,
                Description = "Beautiful luxury apartment"
            },
            new()
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                OwnerId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = "Luxury Penthouse",
                Address = "456 5th Avenue",
                Price = 3500000,
                OperationType = "sale",
                Beds = 4,
                Baths = 3,
                HalfBaths = 1,
                Sqft = 3200,
                Description = "Stunning penthouse with city views"
            },
            new()
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                OwnerId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = "Modern Rental",
                Address = "789 Broadway",
                Price = 1500000,
                OperationType = "rent",
                Beds = 2,
                Baths = 2,
                HalfBaths = 0,
                Sqft = 1800,
                Description = "Modern rental unit"
            }
        };

        await propertiesCollection.InsertManyAsync(properties);

        // Insert test images using actual property IDs
        var images = new List<PropertyImageDocument>
        {
            new() { PropertyId = properties[0].Id, File = "image1.jpg", Enabled = true, Order = 1 },
            new() { PropertyId = properties[0].Id, File = "image2.jpg", Enabled = true, Order = 2 },
            new() { PropertyId = properties[0].Id, File = "image3.jpg", Enabled = false, Order = 3 },
            new() { PropertyId = properties[1].Id, File = "image4.jpg", Enabled = true, Order = 1 },
            new() { PropertyId = properties[1].Id, File = "image5.jpg", Enabled = true, Order = 2 },
            new() { PropertyId = properties[2].Id, File = "image6.jpg", Enabled = true, Order = 1 }
        };

        await imagesCollection.InsertManyAsync(images);
    }

    public async ValueTask DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }
}
