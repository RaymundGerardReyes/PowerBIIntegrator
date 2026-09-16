using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.McpServer.Hosting;
using AnalyticsPlatform.McpServer.Protocol;
using AnalyticsPlatform.McpServer.Security;
using AnalyticsPlatform.McpServer.Tools;
using AnalyticsPlatform.McpServer.Tools.PowerBi;
using AnalyticsPlatform.McpServer.Tools.SecurityAudit;
using Xunit;

namespace AnalyticsPlatform.SecurityTests;

public sealed class McpServerSecurityTests
{
    [Fact]
    public async Task ToolsCall_PrivilegedToolWithoutPrivilege_ReturnsSecurityForbiddenError()
    {
        var mediator = Substitute.For<IMediator>();
        var publishTool = new PublishPbipToFabricTool(mediator, NullLogger<PublishPbipToFabricTool>.Instance);

        var registry = new ToolRegistry(new[] { publishTool }, NullLogger<ToolRegistry>.Instance);
        var permissions = new ToolPermissionMiddleware(NullLogger<ToolPermissionMiddleware>.Instance)
        {
            IsPrivilegedCaller = false
        };

        var audit = new McpAuditLogger(NullLogger<McpAuditLogger>.Instance);
        var host = new StdioMcpServerHost(registry, permissions, audit, NullLogger<StdioMcpServerHost>.Instance);

        var callParams = JsonDocument.Parse($$"""
        {
            "name": "publish_pbip_to_fabric",
            "arguments": {
                "dashboardDefinitionId": "{{Guid.NewGuid()}}",
                "targetWorkspaceId": "ws-123"
            }
        }
        """).RootElement;

        var request = new JsonRpcRequest("2.0", 99, "tools/call", callParams);
        var response = await host.HandleRequestAsync(request, CancellationToken.None);

        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(-32000);
        response.Error.Message.Should().Contain("forbidden by security policy");
    }

    [Fact]
    public async Task ToolsCall_SensitiveToolWhenSensitiveModeDisabled_ReturnsSecurityForbiddenError()
    {
        var mediator = Substitute.For<IMediator>();
        var auditTool = new QueryEventSummaryTool(mediator, NullLogger<QueryEventSummaryTool>.Instance);

        var registry = new ToolRegistry(new[] { auditTool }, NullLogger<ToolRegistry>.Instance);
        var permissions = new ToolPermissionMiddleware(NullLogger<ToolPermissionMiddleware>.Instance)
        {
            AllowSensitive = false
        };

        var audit = new McpAuditLogger(NullLogger<McpAuditLogger>.Instance);
        var host = new StdioMcpServerHost(registry, permissions, audit, NullLogger<StdioMcpServerHost>.Instance);

        var callParams = JsonDocument.Parse("""
        {
            "name": "query_event_summary",
            "arguments": {}
        }
        """).RootElement;

        var request = new JsonRpcRequest("2.0", 100, "tools/call", callParams);
        var response = await host.HandleRequestAsync(request, CancellationToken.None);

        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(-32000);
        response.Error.Message.Should().Contain("forbidden by security policy");
    }
}

