using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AnalyticsPlatform.McpServer.Tools;

public sealed class ToolRegistry
{
    private readonly ConcurrentDictionary<string, IMcpTool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(IEnumerable<IMcpTool> tools, ILogger<ToolRegistry> logger)
    {
        _logger = logger;
        foreach (var tool in tools)
        {
            RegisterTool(tool);
        }
    }

    public void RegisterTool(IMcpTool tool)
    {
        _tools[tool.Name] = tool;
        _logger.LogInformation("[ToolRegistry] Registered MCP Tool: {ToolName}", tool.Name);
    }

    public IReadOnlyCollection<IMcpTool> GetAllTools() => _tools.Values.ToList();

    public bool TryGetTool(string name, out IMcpTool? tool) => _tools.TryGetValue(name, out tool);

    public async Task<McpToolExecutionResult> ExecuteToolAsync(string name, JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        if (!_tools.TryGetValue(name, out var tool))
        {
            return McpToolExecutionResult.Failed($"Tool '{name}' is not registered on this MCP server.");
        }

        return await tool.ExecuteAsync(inputParameters, correlationId, ct);
    }
}
