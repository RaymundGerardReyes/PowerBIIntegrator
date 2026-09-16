using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.Analytics.Commands.ValidateAnalyticsModel;

namespace AnalyticsPlatform.McpServer.Tools.Analytics;

public sealed class ValidateAnalyticsModelTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<ValidateAnalyticsModelTool> _logger;

    public string Name => "validate_analytics_model";
    public string Description => "Validates the semantic consistency of an analytics model (detecting orphan tables, relationship loops, and invalid measures).";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "modelId": { "type": "string", "format": "uuid", "description": "The unique ID of the analytics model to validate" }
      },
      "required": ["modelId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "modelId": { "type": "string" },
        "modelName": { "type": "string" },
        "isValid": { "type": "boolean" },
        "errors": { "type": "array", "items": { "type": "string" } },
        "warnings": { "type": "array", "items": { "type": "string" } },
        "orphanTables": { "type": "array", "items": { "type": "string" } },
        "detectedCycles": { "type": "array", "items": { "type": "string" } }
      }
    }
    """;

    public ValidateAnalyticsModelTool(IMediator mediator, ILogger<ValidateAnalyticsModelTool> logger)
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

        var result = await _mediator.Send(new ValidateAnalyticsModelCommand(modelId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        return McpToolExecutionResult.Success(result.Value);
    }
}

