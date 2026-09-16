using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.Analytics.Queries.GetAnalyticsModel;

namespace AnalyticsPlatform.McpServer.Tools.Analytics;

public sealed class GetAnalyticsModelTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetAnalyticsModelTool> _logger;

    public string Name => "get_analytics_model";
    public string Description => "Retrieves metadata and schema structure of an analytics model by ID.";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "modelId": { "type": "string", "format": "uuid", "description": "The unique ID of the analytics model" }
      },
      "required": ["modelId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "id": { "type": "string" },
        "name": { "type": "string" },
        "culture": { "type": "string" },
        "tables": { "type": "array", "items": { "type": "string" } },
        "measures": { "type": "array", "items": { "type": "string" } },
        "relationshipsCount": { "type": "integer" }
      }
    }
    """;

    public GetAnalyticsModelTool(IMediator mediator, ILogger<GetAnalyticsModelTool> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("[MCP Tool {ToolName}] Executing with CorrelationId: {CorrelationId}", Name, correlationId);

        if (!inputParameters.TryGetProperty("modelId", out var modelIdElem) ||
            !Guid.TryParse(modelIdElem.GetString(), out var modelId))
        {
            return McpToolExecutionResult.Failed("Missing or invalid 'modelId' parameter. Must be a valid UUID.");
        }

        var result = await _mediator.Send(new GetAnalyticsModelQuery(modelId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        return McpToolExecutionResult.Success(result.Value);
    }
}
