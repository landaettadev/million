using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using RealEstate.Api;
using RealEstate.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.Infrastructure;
using MongoDB.Bson;

namespace RealEstate.Tests.Integration.Endpoints;

[TestFixture]
public class AdminImagesEndpointsIntegrationTests
{
    private WebApplicationFactory<RealEstate.Api.Program> _factory = default!;
    private HttpClient _client = default!;

    [SetUp]
    public void Setup()
    {
        _factory = new IntegrationTestWebAppFactory();
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task GetPropertyImages_Returns_List_ForExistingProperty()
    {
        // Seed owner, property and image directly in Mongo to avoid admin create endpoints
        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();

            var ownerId = ObjectId.GenerateNewId().ToString();
            await ctx.Owners.InsertOneAsync(new OwnerDocument
            {
                Id = ownerId,
                Name = "Owner",
                Address = "X",
                CreatedAt = DateTime.UtcNow
            });

            var propertyId = ObjectId.GenerateNewId().ToString();
            await ctx.Properties.InsertOneAsync(new PropertyDocument
            {
                Id = propertyId,
                OwnerId = ownerId,
                Name = "P",
                Address = "X",
                Price = 1,
                OperationType = "sale",
                CreatedAt = DateTime.UtcNow
            });

            await ctx.PropertyImages.InsertOneAsync(new PropertyImageDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                PropertyId = propertyId,
                File = "test.jpg",
                Enabled = true,
                Order = 0,
                FileSize = 1234,
                ContentType = "image/jpeg",
                CreatedAt = DateTime.UtcNow
            });

            // Act
            var res = await _client.GetAsync($"/api/admin/properties/{propertyId}/images");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await res.Content.ReadAsStringAsync();
            body.Should().NotBeNullOrEmpty();

            using var doc = System.Text.Json.JsonDocument.Parse(body);
            doc.RootElement.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
            var arr = doc.RootElement.EnumerateArray().ToArray();
            arr.Length.Should().BeGreaterThanOrEqualTo(1);
            var first = arr[0];
            first.TryGetProperty("propertyId", out _).Should().BeTrue();
            first.TryGetProperty("blobName", out _).Should().BeTrue();
            first.TryGetProperty("imageUrl", out _).Should().BeTrue();
        }
    }
}


