using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.Api;

public class AnalyticsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AnalyticsEndpointsTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task PostMeasure_WithValidPayload_ReturnsOkWithResponseObject()
    {
        var payload = new { Name = "TotalRevenue", Expression = "SUM(Sales[Amount])", TableName = "Sales" };
        var response = await _client.PostAsJsonAsync("/api/analytics/measures", payload);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TotalRevenue");
    }
}
