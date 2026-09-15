using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using Xunit;

namespace AnalyticsPlatform.E2ETests;

public class DataSourceRegistrationE2ETests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    private readonly string _tempCsvFile;

    public DataSourceRegistrationE2ETests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _tempCsvFile = Path.Combine(Path.GetTempPath(), $"e2e_orders_{Guid.NewGuid():N}.csv");
        File.WriteAllText(_tempCsvFile, "OrderId,Product,Revenue,IsActive\n501,Enterprise Server,4500.00,true\n502,Workstation,1200.50,false\n");
    }

    public void Dispose()
    {
        if (File.Exists(_tempCsvFile))
        {
            try { File.Delete(_tempCsvFile); } catch { }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RegisterCsvDataSource_ThenQuerySchema_SucceedsEndToEnd()
    {
        // 1. POST to register data source
        var registerCommand = new
        {
            Name = "ProductionOrdersFeed",
            Type = "Csv",
            ConnectionOrPath = _tempCsvFile
        };

        var postResponse = await _client.PostAsJsonAsync("/api/data-sources/register", registerCommand);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registered = await postResponse.Content.ReadFromJsonAsync<DataSourceResponse>(JsonOptions);
        registered.Should().NotBeNull();
        registered!.Id.Should().NotBeEmpty();
        registered.Name.Should().Be("ProductionOrdersFeed");
        registered.Type.Should().Be("csv");
        registered.Schema.Should().HaveCount(4);

        // 2. GET schema by registered ID
        var schemaResponse = await _client.GetAsync($"/api/data-sources/{registered.Id}/schema");
        schemaResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var schemaList = await schemaResponse.Content.ReadFromJsonAsync<List<ColumnSchema>>(JsonOptions);
        schemaList.Should().NotBeNull();
        var nonNullSchema = schemaList!;
        nonNullSchema.Should().HaveCount(4);

        var orderIdCol = nonNullSchema.First(c => c.Name == "OrderId");
        orderIdCol.InferredType.Should().Be(ColumnDataType.Integer);
        orderIdCol.SampleValues.Should().Contain("501");

        var revenueCol = nonNullSchema.First(c => c.Name == "Revenue");
        revenueCol.InferredType.Should().Be(ColumnDataType.Decimal);

        var activeCol = nonNullSchema.First(c => c.Name == "IsActive");
        activeCol.InferredType.Should().Be(ColumnDataType.Boolean);
    }
}
