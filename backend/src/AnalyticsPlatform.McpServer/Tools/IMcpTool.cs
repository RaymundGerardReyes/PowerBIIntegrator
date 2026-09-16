using System.Text.Json;

namespace AnalyticsPlatform.McpServer.Tools;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    string InputSchemaJson { get; }
    string OutputSchemaJson { get; }
    Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct);
}
