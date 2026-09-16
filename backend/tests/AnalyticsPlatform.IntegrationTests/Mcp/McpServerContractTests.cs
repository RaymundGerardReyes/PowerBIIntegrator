using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.McpServer.Hosting;
using AnalyticsPlatform.McpServer.Protocol;
using AnalyticsPlatform.McpServer.Security;
using AnalyticsPlatform.McpServer.Tools;
using AnalyticsPlatform.McpServer.Tools.Analytics;
using AnalyticsPlatform.McpServer.Tools.Dashboards;
using AnalyticsPlatform.McpServer.Tools.DataSources;
using AnalyticsPlatform.McpServer.Tools.PowerBi;
using AnalyticsPlatform.McpServer.Tools.SecurityAudit;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.Mcp;

public sealed class McpServerContractTests
{
    private readonly StdioMcpServerHost _host;

    public McpServerContractTests()
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

        var registry = new ToolRegistry(tools, NullLogger<ToolRegistry>.Instance);
        var permissions = new ToolPermissionMiddleware(NullLogger<ToolPermissionMiddleware>.Instance);
        var audit = new McpAuditLogger(NullLogger<McpAuditLogger>.Instance);

        _host = new StdioMcpServerHost(registry, permissions, audit, NullLogger<StdioMcpServerHost>.Instance);
    }

    [Fact]
    public async Task Initialize_ReturnsConformingMcpCapabilities()
    {
        var request = new JsonRpcRequest("2.0", 1, "initialize", null);
        var response = await _host.HandleRequestAsync(request, CancellationToken.None);

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().Be(1);
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();

        var initResult = response.Result as McpInitializeResult;
        initResult.Should().NotBeNull();
        initResult!.ProtocolVersion.Should().Be("2024-11-05");
        initResult.ServerInfo.Name.Should().Be("AnalyticsPlatform.McpServer");
        initResult.Capabilities.Tools.Should().NotBeNull();
    }

    [Fact]
    public async Task ToolsList_ReturnsAllNineRegisteredTools()
    {
        var request = new JsonRpcRequest("2.0", 2, "tools/list", null);
        var response = await _host.HandleRequestAsync(request, CancellationToken.None);

        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();

        var json = JsonSerializer.Serialize(response.Result);
        json.Should().Contain("get_analytics_model");
        json.Should().Contain("validate_analytics_model");
        json.Should().Contain("compile_pbir_definition");
        json.Should().Contain("compile_tmdl_model");
        json.Should().Contain("compile_pbip_package");
        json.Should().Contain("publish_pbip_to_fabric");
        json.Should().Contain("get_dashboard_definition");
        json.Should().Contain("get_data_source_schema");
        json.Should().Contain("query_event_summary");
    }

    [Fact]
    public async Task UnknownMethod_ReturnsMethodNotFoundRpcError()
    {
        var request = new JsonRpcRequest("2.0", 3, "unknown_custom_method", null);
        var response = await _host.HandleRequestAsync(request, CancellationToken.None);

        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(JsonRpcError.MethodNotFound);
    }
}
