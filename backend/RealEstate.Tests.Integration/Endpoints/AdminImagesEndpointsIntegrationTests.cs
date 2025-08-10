using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using RealEstate.Api;
using RealEstate.Tests.Integration.Infrastructure;

namespace RealEstate.Tests.Integration.Endpoints;

[TestFixture]
public class AdminImagesEndpointsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AdminImagesEndpointsIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test.token");
    }

    [Test]
    public async Task Presign_And_Delete_Image_Succeeds()
    {
        // Presign
        var presignRes = await _client.PostAsJsonAsync("/api/admin/images/presign", new
        {
            fileName = "test.jpg",
            contentType = "image/jpeg"
        });
        presignRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var presigned = await presignRes.Content.ReadFromJsonAsync<dynamic>();
        presigned!.uploadUrl.Should().NotBeNull();
        string blobName = presigned!.blobName.ToString();

        // Register image metadata (simulate add)
        // Need a property first
        var ownerRes = await _client.PostAsJsonAsync("/api/admin/owners", new { name = "Owner", address = "X" });
        var owner = await ownerRes.Content.ReadFromJsonAsync<dynamic>();
        string ownerId = owner!.id.ToString();
        var propRes = await _client.PostAsJsonAsync("/api/admin/properties", new { ownerId, name = "P", address = "X", price = 1, operationType = "Sale" });
        var prop = await propRes.Content.ReadFromJsonAsync<dynamic>();
        string propertyId = prop!.id.ToString();

        var addImageRes = await _client.PostAsJsonAsync("/api/admin/images", new
        {
            propertyId,
            file = blobName,
            enabled = true,
            order = 0
        });
        addImageRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var img = await addImageRes.Content.ReadFromJsonAsync<dynamic>();
        string imageId = img!.id.ToString();

        // Delete image
        var delRes = await _client.DeleteAsync($"/api/admin/images/{imageId}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}


