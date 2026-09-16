using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbipProject;

namespace AnalyticsPlatform.McpServer.Tools.PowerBi;

public sealed class CompilePbipPackageTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompilePbipPackageTool> _logger;

    public string Name => "compile_pbip_package";
    public string Description => "Compiles both TMDL semantic model and PBIR report into a complete Power BI Project (.pbip) folder structure and manifest.";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "dashboardDefinitionId": { "type": "string", "format": "uuid", "description": "The unique ID of the dashboard definition" },
        "analyticsModelId": { "type": "string", "format": "uuid", "description": "The unique ID of the analytics model" },
        "projectName": { "type": "string", "description": "Optional project folder name" }
      },
      "required": ["dashboardDefinitionId", "analyticsModelId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "projectName": { "type": "string" },
        "totalFiles": { "type": "integer" },
        "manifest": { "type": "object" }
      }
    }
    """;

    public CompilePbipPackageTool(IMediator mediator, ILogger<CompilePbipPackageTool> logger)
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

        if (!inputParameters.TryGetProperty("analyticsModelId", out var mIdElem) ||
            !Guid.TryParse(mIdElem.GetString(), out var modelId))
        {
            return McpToolExecutionResult.Failed("Missing or invalid 'analyticsModelId' parameter.");
        }

        var projectName = inputParameters.TryGetProperty("projectName", out var pElem)
            ? pElem.GetString()
            : "AnalyticsPlatformProject";

        var command = new CompilePbipProjectCommand(dashboardId, modelId, projectName);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess || result.Value == null)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        return McpToolExecutionResult.Success(result.Value);
    }
}

