using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.DataSources.Queries.GetDataSourceSchema;

namespace AnalyticsPlatform.McpServer.Tools.DataSources;

public sealed class GetDataSourceSchemaTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetDataSourceSchemaTool> _logger;

    public string Name => "get_data_source_schema";
    public string Description => "Retrieves the extracted column schema (names, inferred types, nullability) of a registered data source.";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "dataSourceId": { "type": "string", "format": "uuid", "description": "The unique ID of the data source" }
      },
      "required": ["dataSourceId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "columns": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "name": { "type": "string" },
              "dataType": { "type": "string" },
              "isNullable": { "type": "boolean" },
              "sampleValues": { "type": "array", "items": { "type": "string" } }
            }
          }
        }
      }
    }
    """;

    public GetDataSourceSchemaTool(IMediator mediator, ILogger<GetDataSourceSchemaTool> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("[MCP Tool {ToolName}] Executing with CorrelationId: {CorrelationId}", Name, correlationId);

        if (!inputParameters.TryGetProperty("dataSourceId", out var idElem) ||
            !Guid.TryParse(idElem.GetString(), out var dataSourceId))
        {
            return McpToolExecutionResult.Failed("Missing or invalid 'dataSourceId' parameter. Must be a valid UUID.");
        }

        var result = await _mediator.Send(new GetDataSourceSchemaQuery(dataSourceId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        var columnsDto = result.Value.Select(c => new
        {
            name = c.Name,
            dataType = c.InferredType.ToString(),
            isNullable = c.IsNullable,
            sampleValues = c.SampleValues
        });

        return McpToolExecutionResult.Success(new { columns = columnsDto });
    }
}
