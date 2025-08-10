using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using RealEstate.Api;
using RealEstate.Tests.Integration.Infrastructure;

namespace RealEstate.Tests.Integration.Endpoints;

[TestFixture]
public class AdminPropertiesEndpointsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AdminPropertiesEndpointsIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test.token");
    }

    [Test]
    public async Task Create_Update_Delete_Restore_Property_Succeeds()
    {
        // Create owner first
        var ownerRes = await _client.PostAsJsonAsync("/api/admin/owners", new
        {
            name = "Owner Test",
            address = "123 Test St"
        });
        ownerRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var owner = await ownerRes.Content.ReadFromJsonAsync<dynamic>();
        string ownerId = owner!.id.ToString();

        // Create property
        var createRes = await _client.PostAsJsonAsync("/api/admin/properties", new
        {
            ownerId,
            name = "Prop Test",
            address = "123 Test St",
            price = 1000000,
            operationType = "Sale"
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<dynamic>();
        string propertyId = created!.id.ToString();

        // Update
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/properties/{propertyId}", new
        {
            name = "Prop Test Updated",
            address = "123 Test St",
            price = 1100000,
            operationType = "Sale"
        });
        updateRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Delete (soft)
        var delRes = await _client.DeleteAsync($"/api/admin/properties/{propertyId}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore
        var restoreRes = await _client.PostAsync($"/api/admin/properties/{propertyId}/restore", null);
        restoreRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}


