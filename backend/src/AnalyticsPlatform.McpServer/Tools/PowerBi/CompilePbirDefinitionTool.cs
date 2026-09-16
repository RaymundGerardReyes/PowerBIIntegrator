using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbirDefinition;

namespace AnalyticsPlatform.McpServer.Tools.PowerBi;

public sealed class CompilePbirDefinitionTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompilePbirDefinitionTool> _logger;

    public string Name => "compile_pbir_definition";
    public string Description => "Compiles an analytics dashboard IR into PBIR report definition files (report.json, pages, visuals).";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "dashboardDefinitionId": { "type": "string", "format": "uuid", "description": "The unique ID of the dashboard definition" },
        "semanticModelRelativePath": { "type": "string", "description": "Relative path to TMDL semantic model" }
      },
      "required": ["dashboardDefinitionId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "success": { "type": "boolean" },
        "dashboardName": { "type": "string" },
        "totalFiles": { "type": "integer" },
        "reportJsonPath": { "type": "string" }
      }
    }
    """;

    public CompilePbirDefinitionTool(IMediator mediator, ILogger<CompilePbirDefinitionTool> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("[MCP Tool {ToolName}] Executing with CorrelationId: {CorrelationId}", Name, correlationId);

        if (!inputParameters.TryGetProperty("dashboardDefinitionId", out var idElem) ||
            !Guid.TryParse(idElem.GetString(), out var dashboardId))
        {
            return McpToolExecutionResult.Failed("Missing or invalid 'dashboardDefinitionId' parameter. Must be a valid UUID.");
        }

        var modelPath = inputParameters.TryGetProperty("semanticModelRelativePath", out var pathElem)
            ? pathElem.GetString()
            : "../definition";

        var command = new CompilePbirDefinitionCommand(dashboardId, modelPath);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess || result.Value == null)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        return McpToolExecutionResult.Success(new
        {
            success = true,
            dashboardName = result.Value.DashboardName,
            totalFiles = result.Value.TotalFiles,
            reportJsonPath = "definition/report.json"
        });
    }
}

