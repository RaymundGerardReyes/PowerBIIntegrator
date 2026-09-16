using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.SecurityAudit.Queries.QueryEventSummary;

namespace AnalyticsPlatform.McpServer.Tools.SecurityAudit;

public sealed class QueryEventSummaryTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<QueryEventSummaryTool> _logger;

    public string Name => "query_event_summary";
    public string Description => "Sensitive Context: Queries aggregate security audit metrics, guardrail violation counts, and LLM provider execution distributions.";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "sinceUtc": { "type": "string", "format": "date-time", "description": "Optional lower timestamp bound" },
        "severityFilter": { "type": "string", "description": "Optional severity filter" }
      }
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "totalEventsAnalyzed": { "type": "integer" },
        "blockedPromptCount": { "type": "integer" },
        "sanitizedPromptCount": { "type": "integer" },
        "policyViolationsCount": { "type": "integer" },
        "providerExecutionCounts": { "type": "object" },
        "recentSecurityAlerts": { "type": "array", "items": { "type": "string" } }
      }
    }
    """;

    public QueryEventSummaryTool(IMediator mediator, ILogger<QueryEventSummaryTool> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("[MCP Tool {ToolName}] Executing with CorrelationId: {CorrelationId}", Name, correlationId);

        DateTime? sinceUtc = null;
        if (inputParameters.TryGetProperty("sinceUtc", out var sinceElem) &&
            DateTime.TryParse(sinceElem.GetString(), out var parsedDate))
        {
            sinceUtc = parsedDate;
        }

        string? severity = inputParameters.TryGetProperty("severityFilter", out var sevElem)
            ? sevElem.GetString()
            : null;

        var result = await _mediator.Send(new QueryEventSummaryQuery(sinceUtc, severity), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        return McpToolExecutionResult.Success(result.Value);
    }
}

