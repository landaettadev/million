using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using RealEstate.Api;
using RealEstate.Tests.Integration.Infrastructure;

namespace RealEstate.Tests.Integration.Endpoints;

[TestFixture]
public class AdminOwnersEndpointsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AdminOwnersEndpointsIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test.token");
    }

    [Test]
    public async Task Create_Update_Delete_Owner_Succeeds()
    {
        // Create owner
        var createRes = await _client.PostAsJsonAsync("/api/admin/owners", new
        {
            name = "Owner Test",
            address = "123 Test St"
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<dynamic>();
        string ownerId = created!.id.ToString();

        // Update
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/owners/{ownerId}", new
        {
            name = "Owner Test Updated",
            address = "456 Test Ave"
        });
        updateRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Delete (soft)
        var delRes = await _client.DeleteAsync($"/api/admin/owners/{ownerId}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}


