using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.Dashboards.Queries.GetDashboardDefinition;

namespace AnalyticsPlatform.McpServer.Tools.Dashboards;

public sealed class GetDashboardDefinitionTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetDashboardDefinitionTool> _logger;

    public string Name => "get_dashboard_definition";
    public string Description => "Retrieves an analytics dashboard definition by ID including its pages, visual structures, and bound model.";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "dashboardId": { "type": "string", "format": "uuid", "description": "The unique ID of the dashboard definition" }
      },
      "required": ["dashboardId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "id": { "type": "string" },
        "name": { "type": "string" },
        "analyticsModelId": { "type": "string" },
        "pages": { "type": "array" },
        "totalVisuals": { "type": "integer" }
      }
    }
    """;

    public GetDashboardDefinitionTool(IMediator mediator, ILogger<GetDashboardDefinitionTool> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("[MCP Tool {ToolName}] Executing with CorrelationId: {CorrelationId}", Name, correlationId);

        if (!inputParameters.TryGetProperty("dashboardId", out var idElem) ||
            !Guid.TryParse(idElem.GetString(), out var dashboardId))
        {
            return McpToolExecutionResult.Failed("Missing or invalid 'dashboardId' parameter. Must be a valid UUID.");
        }

        var result = await _mediator.Send(new GetDashboardDefinitionQuery(dashboardId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        return McpToolExecutionResult.Success(result.Value);
    }
}

