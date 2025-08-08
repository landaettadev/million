using FluentAssertions;
using NSubstitute;
using RealEstate.Application;
using Xunit;

namespace RealEstate.Tests.Services;

public class PropertyReadServiceTests
{
    private readonly IPropertyRepository _mockRepository;
    private readonly PropertyReadService _service;

    public PropertyReadServiceTests()
    {
        _mockRepository = Substitute.For<IPropertyRepository>();
        _service = new PropertyReadService(_mockRepository);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ShouldCallRepositoryAndReturnResult()
    {
        // Arrange
        var query = new SearchPropertiesQuery(
            Name: "Luxury",
            Address: "Manhattan",
            MinPrice: 1000000,
            MaxPrice: 5000000,
            OperationType: Application.OperationType.Sale,
            Page: 2,
            PageSize: 10
        );

        var expectedResult = new PagedResult<PropertyLiteDto>(
            new List<PropertyLiteDto>
            {
                new("1", "owner1", "Luxury Apartment", "123 Park Ave", 2500000, "image1.jpg", Application.OperationType.Sale, 3, 2, 1, 2500),
                new("2", "owner2", "Penthouse Suite", "456 5th Ave", 3500000, "image2.jpg", Application.OperationType.Sale, 4, 3, 1, 3200)
            },
            2, 10, 15
        );

        _mockRepository.SearchAsync(Arg.Any<SearchPropertiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _service.SearchAsync(query);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        await _mockRepository.Received(1).SearchAsync(Arg.Is<SearchPropertiesQuery>(q => 
            q.Name == query.Name &&
            q.Address == query.Address &&
            q.MinPrice == query.MinPrice &&
            q.MaxPrice == query.MaxPrice &&
            q.OperationType == query.OperationType &&
            q.Page == query.Page &&
            q.PageSize == query.PageSize
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithZeroPage_ShouldNormalizeToDefaultPage()
    {
        // Arrange
        var query = new SearchPropertiesQuery(Page: 0, PageSize: 20);
        var expectedNormalizedQuery = new SearchPropertiesQuery(Page: 1, PageSize: 20);

        _mockRepository.SearchAsync(Arg.Any<SearchPropertiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<PropertyLiteDto>(new List<PropertyLiteDto>(), 1, 20, 0));

        // Act
        await _service.SearchAsync(query);

        // Assert
        await _mockRepository.Received(1).SearchAsync(Arg.Is<SearchPropertiesQuery>(q => 
            q.Page == expectedNormalizedQuery.Page &&
            q.PageSize == expectedNormalizedQuery.PageSize
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithNegativePage_ShouldNormalizeToDefaultPage()
    {
        // Arrange
        var query = new SearchPropertiesQuery(Page: -5, PageSize: 20);
        var expectedNormalizedQuery = new SearchPropertiesQuery(Page: 1, PageSize: 20);

        _mockRepository.SearchAsync(Arg.Any<SearchPropertiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<PropertyLiteDto>(new List<PropertyLiteDto>(), 1, 20, 0));

        // Act
        await _service.SearchAsync(query);

        // Assert
        await _mockRepository.Received(1).SearchAsync(Arg.Is<SearchPropertiesQuery>(q => 
            q.Page == expectedNormalizedQuery.Page &&
            q.PageSize == expectedNormalizedQuery.PageSize
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithZeroPageSize_ShouldNormalizeToDefaultPageSize()
    {
        // Arrange
        var query = new SearchPropertiesQuery(Page: 1, PageSize: 0);
        var expectedNormalizedQuery = new SearchPropertiesQuery(Page: 1, PageSize: 20);

        _mockRepository.SearchAsync(Arg.Any<SearchPropertiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<PropertyLiteDto>(new List<PropertyLiteDto>(), 1, 20, 0));

        // Act
        await _service.SearchAsync(query);

        // Assert
        await _mockRepository.Received(1).SearchAsync(Arg.Is<SearchPropertiesQuery>(q => 
            q.Page == expectedNormalizedQuery.Page &&
            q.PageSize == expectedNormalizedQuery.PageSize
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithPageSizeExceedingMax_ShouldNormalizeToMaxPageSize()
    {
        // Arrange
        var query = new SearchPropertiesQuery(Page: 1, PageSize: 100);
        var expectedNormalizedQuery = new SearchPropertiesQuery(Page: 1, PageSize: 50);

        _mockRepository.SearchAsync(Arg.Any<SearchPropertiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<PropertyLiteDto>(new List<PropertyLiteDto>(), 1, 50, 0));

        // Act
        await _service.SearchAsync(query);

        // Assert
        await _mockRepository.Received(1).SearchAsync(Arg.Is<SearchPropertiesQuery>(q => 
            q.Page == expectedNormalizedQuery.Page &&
            q.PageSize == expectedNormalizedQuery.PageSize
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithNegativePageSize_ShouldNormalizeToDefaultPageSize()
    {
        // Arrange
        var query = new SearchPropertiesQuery(Page: 1, PageSize: -10);
        var expectedNormalizedQuery = new SearchPropertiesQuery(Page: 1, PageSize: 20);

        _mockRepository.SearchAsync(Arg.Any<SearchPropertiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<PropertyLiteDto>(new List<PropertyLiteDto>(), 1, 20, 0));

        // Act
        await _service.SearchAsync(query);

        // Assert
        await _mockRepository.Received(1).SearchAsync(Arg.Is<SearchPropertiesQuery>(q => 
            q.Page == expectedNormalizedQuery.Page &&
            q.PageSize == expectedNormalizedQuery.PageSize
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldCallRepositoryAndReturnResult()
    {
        // Arrange
        var id = "property123";
        var expectedProperty = new PropertyDetailDto(
            id, "owner1", "Luxury Apartment", "123 Park Ave", 2500000,
            new List<string> { "image1.jpg", "image2.jpg" },
            Application.OperationType.Sale, "Beautiful luxury apartment", 3, 2, 1, 2500
        );

        _mockRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(expectedProperty);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().BeEquivalentTo(expectedProperty);
        await _mockRepository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var id = "nonexistent";
        _mockRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((PropertyDetailDto?)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
        await _mockRepository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ShouldPassEmptyQueryToRepository()
    {
        // Arrange
        var query = new SearchPropertiesQuery();
        var expectedResult = new PagedResult<PropertyLiteDto>(new List<PropertyLiteDto>(), 1, 20, 0);

        _mockRepository.SearchAsync(Arg.Any<SearchPropertiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        await _service.SearchAsync(query);

        // Assert
        await _mockRepository.Received(1).SearchAsync(Arg.Is<SearchPropertiesQuery>(q => 
            q.Name == null &&
            q.Address == null &&
            q.MinPrice == null &&
            q.MaxPrice == null &&
            q.OperationType == null &&
            q.Page == 1 &&
            q.PageSize == 20
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithCancellationToken_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var query = new SearchPropertiesQuery();
        var cancellationToken = new CancellationToken(true);
        var expectedResult = new PagedResult<PropertyLiteDto>(new List<PropertyLiteDto>(), 1, 20, 0);

        _mockRepository.SearchAsync(Arg.Any<SearchPropertiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        await _service.SearchAsync(query, cancellationToken);

        // Assert
        await _mockRepository.Received(1).SearchAsync(Arg.Any<SearchPropertiesQuery>(), cancellationToken);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var id = "property123";
        var cancellationToken = new CancellationToken(true);
        var expectedProperty = new PropertyDetailDto(
            id, "owner1", "Luxury Apartment", "123 Park Ave", 2500000,
            new List<string> { "image1.jpg" },
            Application.OperationType.Sale
        );

        _mockRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(expectedProperty);

        // Act
        await _service.GetByIdAsync(id, cancellationToken);

        // Assert
        await _mockRepository.Received(1).GetByIdAsync(id, cancellationToken);
    }
}
