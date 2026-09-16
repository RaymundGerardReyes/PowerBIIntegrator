using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.McpServer.Tools;
using AnalyticsPlatform.McpServer.Tools.Analytics;
using AnalyticsPlatform.McpServer.Tools.Dashboards;
using AnalyticsPlatform.McpServer.Tools.DataSources;
using AnalyticsPlatform.McpServer.Tools.PowerBi;
using AnalyticsPlatform.McpServer.Tools.SecurityAudit;
using Xunit;

namespace AnalyticsPlatform.RegressionTests;

public sealed partial class McpToolSchemasRegressionTests
{
    [GeneratedRegex("^[a-z0-9_]+$")]
    private static partial Regex ToolNameRegex();

    [Fact]
    public void AllMcpTools_ConformToMcpSchemaStandards()
    {
        var mediator = Substitute.For<IMediator>();

        var tools = new IMcpTool[]
        {
            new GetAnalyticsModelTool(mediator, NullLogger<GetAnalyticsModelTool>.Instance),
            new ValidateAnalyticsModelTool(mediator, NullLogger<ValidateAnalyticsModelTool>.Instance),
            new CompilePbirDefinitionTool(mediator, NullLogger<CompilePbirDefinitionTool>.Instance),
            new CompileTmdlSemanticModelTool(mediator, NullLogger<CompileTmdlSemanticModelTool>.Instance),
            new CompilePbipPackageTool(mediator, NullLogger<CompilePbipPackageTool>.Instance),
            new PublishPbipToFabricTool(mediator, NullLogger<PublishPbipToFabricTool>.Instance),
            new GetDashboardDefinitionTool(mediator, NullLogger<GetDashboardDefinitionTool>.Instance),
            new GetDataSourceSchemaTool(mediator, NullLogger<GetDataSourceSchemaTool>.Instance),
            new QueryEventSummaryTool(mediator, NullLogger<QueryEventSummaryTool>.Instance)
        };

        foreach (var tool in tools)
        {
            // Name must be non-empty snake_case
            tool.Name.Should().NotBeNullOrWhiteSpace();
            ToolNameRegex().IsMatch(tool.Name).Should().BeTrue($"tool name '{tool.Name}' must be snake_case");

            // Description must be meaningful
            tool.Description.Should().NotBeNullOrWhiteSpace();
            tool.Description.Length.Should().BeGreaterThan(15);

            // Input Schema must be valid JSON object with properties
            var inputDoc = JsonDocument.Parse(tool.InputSchemaJson);
            inputDoc.RootElement.GetProperty("type").GetString().Should().Be("object");
            inputDoc.RootElement.TryGetProperty("properties", out _).Should().BeTrue();

            // Output Schema must be valid JSON object with properties
            var outputDoc = JsonDocument.Parse(tool.OutputSchemaJson);
            outputDoc.RootElement.GetProperty("type").GetString().Should().Be("object");
            outputDoc.RootElement.TryGetProperty("properties", out _).Should().BeTrue();
        }
    }
}

