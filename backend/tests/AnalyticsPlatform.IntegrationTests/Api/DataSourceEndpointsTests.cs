using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.Api;

public class DataSourceEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    private readonly string _tempCsvPath;

    public DataSourceEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _tempCsvPath = Path.Combine(Path.GetTempPath(), $"test_integration_{Guid.NewGuid():N}.csv");
        File.WriteAllText(_tempCsvPath, "Id,Product,Price,InStock\n1,Widget,19.99,true\n2,Gadget,49.50,false\n");
    }

    public void Dispose()
    {
        if (File.Exists(_tempCsvPath))
        {
            try { File.Delete(_tempCsvPath); } catch { }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PostRegister_WithCsvFile_ReturnsCreatedWithExtractedSchema()
    {
        var command = new
        {
            Name = "ProductsCatalog",
            Type = "Csv",
            ConnectionOrPath = _tempCsvPath
        };

        var response = await _client.PostAsJsonAsync("/api/data-sources/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<DataSourceResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Name.Should().Be("ProductsCatalog");
        body.Type.Should().Be("csv");
        body.Schema.Should().HaveCount(4);
        body.Schema.Select(s => s.Name).Should().Contain(new[] { "Id", "Product", "Price", "InStock" });
    }

    [Fact]
    public async Task GetSchema_ForRegisteredDataSource_ReturnsOkWithColumnSchemas()
    {
        var command = new
        {
            Name = "SchemaTestCatalog",
            Type = "Csv",
            ConnectionOrPath = _tempCsvPath
        };

        var postResponse = await _client.PostAsJsonAsync("/api/data-sources/register", command);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await postResponse.Content.ReadFromJsonAsync<DataSourceResponse>(JsonOptions);
        created.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/api/data-sources/{created!.Id}/schema");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var schema = await getResponse.Content.ReadFromJsonAsync<List<ColumnSchema>>(JsonOptions);
        schema.Should().NotBeNull();
        var nonNullSchema = schema!;
        nonNullSchema.Should().HaveCount(4);
        nonNullSchema.First(c => c.Name == "Id").InferredType.Should().Be(ColumnDataType.Integer);
        nonNullSchema.First(c => c.Name == "Price").InferredType.Should().Be(ColumnDataType.Decimal);
    }

    [Fact]
    public async Task GetSchema_WhenDataSourceDoesNotExist_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/data-sources/{nonExistentId}/schema");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostUpload_WithLegacyEndpoint_ReturnsOkWithExtractedSchema()
    {
        var request = new
        {
            Name = "UploadedCsv",
            Type = "csv",
            ConnectionOrPath = _tempCsvPath
        };

        var response = await _client.PostAsJsonAsync("/api/data-sources/upload", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DataSourceResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Schema.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostUpload_WithMultipartFormData_ReturnsOkWithExtractedSchema()
    {
        using var form = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(_tempCsvPath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "test_upload.csv");
        form.Add(new StringContent("csv"), "type");

        var response = await _client.PostAsync("/api/data-sources/upload", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DataSourceResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Name.Should().Be("test_upload.csv");
        body.Type.Should().Be("csv");
        body.Schema.Should().NotBeEmpty();
    }
}

