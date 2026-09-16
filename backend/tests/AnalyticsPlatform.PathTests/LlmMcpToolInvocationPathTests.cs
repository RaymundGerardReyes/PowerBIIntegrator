using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.Application.Features.Analytics.Commands.ValidateAnalyticsModel;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.McpServer.Hosting;
using AnalyticsPlatform.McpServer.Protocol;
using AnalyticsPlatform.McpServer.Security;
using AnalyticsPlatform.McpServer.Tools;
using AnalyticsPlatform.McpServer.Tools.Analytics;
using Xunit;

namespace AnalyticsPlatform.PathTests;

public sealed class LlmMcpToolInvocationPathTests
{
    [Fact]
    public async Task StdioHost_ToolInvocationPath_ExecutesAndReturnsConformingResult()
    {
        var mediator = Substitute.For<IMediator>();
        var modelId = Guid.NewGuid();

        var validateResponse = new ValidateAnalyticsModelResponse(
            ModelId: modelId,
            ModelName: "SalesModel",
            IsValid: true,
            Errors: Array.Empty<string>(),
            Warnings: Array.Empty<string>(),
            OrphanTables: Array.Empty<string>(),
            DetectedCycles: Array.Empty<string>()
        );

        mediator.Send(Arg.Any<ValidateAnalyticsModelCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ValidateAnalyticsModelResponse>.Success(validateResponse));

        var tool = new ValidateAnalyticsModelTool(mediator, NullLogger<ValidateAnalyticsModelTool>.Instance);
        var registry = new ToolRegistry(new[] { tool }, NullLogger<ToolRegistry>.Instance);
        var permissions = new ToolPermissionMiddleware(NullLogger<ToolPermissionMiddleware>.Instance);
        var audit = new McpAuditLogger(NullLogger<McpAuditLogger>.Instance);
        var host = new StdioMcpServerHost(registry, permissions, audit, NullLogger<StdioMcpServerHost>.Instance);

        var toolCallParams = JsonDocument.Parse($$"""
        {
            "name": "validate_analytics_model",
            "arguments": {
                "modelId": "{{modelId}}"
            }
        }
        """).RootElement;

        var request = new JsonRpcRequest("2.0", 42, "tools/call", toolCallParams);

        var response = await host.HandleRequestAsync(request, CancellationToken.None);

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().Be(42);
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();

        var resultObj = response.Result as McpToolExecutionResult;
        resultObj.Should().NotBeNull();
        resultObj!.IsError.Should().BeFalse();
        resultObj.Content[0].Text.Should().Contain("SalesModel");
        resultObj.Content[0].Text.Should().Contain("\"isValid\":true");
    }
}
