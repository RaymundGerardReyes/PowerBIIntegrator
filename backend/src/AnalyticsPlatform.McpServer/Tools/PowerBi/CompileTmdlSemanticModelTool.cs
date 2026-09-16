using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompileTmdlSemanticModel;

namespace AnalyticsPlatform.McpServer.Tools.PowerBi;

public sealed class CompileTmdlSemanticModelTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompileTmdlSemanticModelTool> _logger;

    public string Name => "compile_tmdl_model";
    public string Description => "Compiles an analytics model into human-readable TMDL semantic model definition files (model.tmdl, tables/*.tmdl, relationships.tmdl).";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "analyticsModelId": { "type": "string", "format": "uuid", "description": "The unique ID of the analytics model" }
      },
      "required": ["analyticsModelId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "success": { "type": "boolean" },
        "modelName": { "type": "string" },
        "totalFiles": { "type": "integer" }
      }
    }
    """;

    public CompileTmdlSemanticModelTool(IMediator mediator, ILogger<CompileTmdlSemanticModelTool> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("[MCP Tool {ToolName}] Executing with CorrelationId: {CorrelationId}", Name, correlationId);

        if (!inputParameters.TryGetProperty("analyticsModelId", out var idElem) ||
            !Guid.TryParse(idElem.GetString(), out var modelId))
        {
            return McpToolExecutionResult.Failed("Missing or invalid 'analyticsModelId' parameter. Must be a valid UUID.");
        }

        var command = new CompileTmdlSemanticModelCommand(modelId);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess || result.Value == null)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        return McpToolExecutionResult.Success(new
        {
            success = true,
            modelName = result.Value.ModelName,
            totalFiles = result.Value.TotalFiles
        });
    }
}

