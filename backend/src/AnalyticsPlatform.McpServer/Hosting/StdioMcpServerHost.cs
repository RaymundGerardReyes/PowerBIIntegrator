using System.Text.Json;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.McpServer.Protocol;
using AnalyticsPlatform.McpServer.Security;
using AnalyticsPlatform.McpServer.Tools;

namespace AnalyticsPlatform.McpServer.Hosting;

public sealed class StdioMcpServerHost
{
    private readonly ToolRegistry _toolRegistry;
    private readonly ToolPermissionMiddleware _permissionMiddleware;
    private readonly McpAuditLogger _auditLogger;
    private readonly ILogger<StdioMcpServerHost> _logger;

    public StdioMcpServerHost(
        ToolRegistry toolRegistry,
        ToolPermissionMiddleware permissionMiddleware,
        McpAuditLogger auditLogger,
        ILogger<StdioMcpServerHost> logger)
    {
        _toolRegistry = toolRegistry;
        _permissionMiddleware = permissionMiddleware;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("[StdioMcpServerHost] Starting Stdio transport loop (JSON-RPC 2.0)...");

        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            JsonRpcResponse response;
            try
            {
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);
                if (request == null)
                {
                    response = JsonRpcResponse.FromError(null, JsonRpcError.InvalidRequest, "Invalid JSON-RPC request envelope.");
                }
                else
                {
                    response = await HandleRequestAsync(request, ct);
                }
            }
            catch (JsonException ex)
            {
                response = JsonRpcResponse.FromError(null, JsonRpcError.ParseError, $"Parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                response = JsonRpcResponse.FromError(null, JsonRpcError.InternalError, $"Internal error: {ex.Message}");
            }

            var serializedResponse = JsonSerializer.Serialize(response);
            await writer.WriteLineAsync(serializedResponse.AsMemory(), ct);
        }
    }

    public async Task<JsonRpcResponse> HandleRequestAsync(JsonRpcRequest request, CancellationToken ct)
    {
        switch (request.Method)
        {
            case "initialize":
                var initResult = new McpInitializeResult(
                    ProtocolVersion: "2024-11-05",
                    Capabilities: new McpServerCapabilities(
                        Tools: new { listChanged = false },
                        Resources: new { subscribe = false },
                        Prompts: new { listChanged = false }
                    ),
                    ServerInfo: new McpServerInfo("AnalyticsPlatform.McpServer", "1.0.0")
                );
                return JsonRpcResponse.Success(request.Id, initResult);

            case "ping":
                return JsonRpcResponse.Success(request.Id, new { });

            case "tools/list":
                var tools = _toolRegistry.GetAllTools().Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    inputSchema = JsonDocument.Parse(t.InputSchemaJson).RootElement
                });
                return JsonRpcResponse.Success(request.Id, new { tools });

            case "tools/call":
                if (!request.Params.HasValue)
                {
                    return JsonRpcResponse.FromError(request.Id, JsonRpcError.InvalidParams, "Missing 'params' in tools/call request.");
                }

                var paramObj = request.Params.Value;
                if (!paramObj.TryGetProperty("name", out var toolNameElem))
                {
                    return JsonRpcResponse.FromError(request.Id, JsonRpcError.InvalidParams, "Missing tool 'name' parameter.");
                }

                var toolName = toolNameElem.GetString() ?? string.Empty;
                var arguments = paramObj.TryGetProperty("arguments", out var argElem) ? argElem : default;
                var correlationId = Guid.NewGuid().ToString();

                if (!_permissionMiddleware.AuthorizeToolInvocation(toolName, correlationId))
                {
                    return JsonRpcResponse.FromError(request.Id, -32000, $"Execution of tool '{toolName}' is forbidden by security policy.");
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var execResult = await _toolRegistry.ExecuteToolAsync(toolName, arguments, correlationId, ct);
                sw.Stop();

                _auditLogger.LogInvocation(toolName, correlationId, !execResult.IsError, sw.ElapsedMilliseconds);

                return JsonRpcResponse.Success(request.Id, execResult);

            default:
                return JsonRpcResponse.FromError(request.Id, JsonRpcError.MethodNotFound, $"Method '{request.Method}' not found.");
        }
    }
}

