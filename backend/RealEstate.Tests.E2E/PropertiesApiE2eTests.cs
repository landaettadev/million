using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace RealEstate.Tests.E2E;

public class PropertiesApiE2eTests : IClassFixture<E2eFixture>
{
    private readonly E2eFixture _fx;
    public PropertiesApiE2eTests(E2eFixture fx) => _fx = fx;

    [Fact]
    public async Task List_Properties_DefaultPagination_ShouldReturnItems()
    {
        var resp = await _fx.Client.GetAsync("/api/properties");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        json.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task List_Properties_WithFilters_ShouldFilter()
    {
        var resp = await _fx.Client.GetAsync("/api/properties?name=Property&minPrice=100000");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("items").EnumerateArray().All(x => x.GetProperty("price").GetDecimal() >= 100000).Should().BeTrue();
    }

    [Fact]
    public async Task Get_Property_ById_ShouldReturnDetail()
    {
        // first, list to get an id
        var list = await _fx.Client.GetFromJsonAsync<JsonElement>("/api/properties");
        var firstId = list.GetProperty("items").EnumerateArray().First().GetProperty("id").GetString();
        var resp = await _fx.Client.GetAsync($"/api/properties/{firstId}");
        resp.IsSuccessStatusCode.Should().BeTrue();
        var detail = await resp.Content.ReadFromJsonAsync<JsonElement>();
        detail.GetProperty("id").GetString().Should().Be(firstId);
        detail.GetProperty("images").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_Property_InvalidId_ShouldReturn400()
    {
        var resp = await _fx.Client.GetAsync("/api/properties/invalid");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}


