using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.PublishPbipToFabric;

namespace AnalyticsPlatform.McpServer.Tools.PowerBi;

public sealed class PublishPbipToFabricTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<PublishPbipToFabricTool> _logger;

    public string Name => "publish_pbip_to_fabric";
    public string Description => "Privileged operation: Compiles and publishes a PBIP analytics report directly into a Microsoft Fabric / Power BI workspace.";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "dashboardDefinitionId": { "type": "string", "format": "uuid", "description": "The unique ID of the dashboard definition" },
        "targetWorkspaceId": { "type": "string", "description": "Target Fabric / Power BI Workspace GUID" }
      },
      "required": ["dashboardDefinitionId", "targetWorkspaceId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "success": { "type": "boolean" },
        "reportId": { "type": "string" },
        "targetWorkspaceId": { "type": "string" }
      }
    }
    """;

    public PublishPbipToFabricTool(IMediator mediator, ILogger<PublishPbipToFabricTool> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("[MCP Tool {ToolName}] Executing with CorrelationId: {CorrelationId}", Name, correlationId);

        if (!inputParameters.TryGetProperty("dashboardDefinitionId", out var dIdElem) ||
            !Guid.TryParse(dIdElem.GetString(), out var dashboardId))
        {
            return McpToolExecutionResult.Failed("Missing or invalid 'dashboardDefinitionId' parameter.");
        }

        if (!inputParameters.TryGetProperty("targetWorkspaceId", out var wIdElem) ||
            string.IsNullOrWhiteSpace(wIdElem.GetString()))
        {
            return McpToolExecutionResult.Failed("Missing or empty 'targetWorkspaceId' parameter.");
        }

        var workspaceId = wIdElem.GetString()!;
        var command = new PublishPbipToFabricCommand(dashboardId, workspaceId);
        var result = await _mediator.Send(command, ct);

        return McpToolExecutionResult.Success(new
        {
            success = true,
            reportId = result.ReportId,
            targetWorkspaceId = workspaceId
        });
    }
}
