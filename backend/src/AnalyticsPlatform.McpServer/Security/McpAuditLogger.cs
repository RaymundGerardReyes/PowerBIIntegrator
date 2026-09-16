using Microsoft.Extensions.Logging;

namespace AnalyticsPlatform.McpServer.Security;

public sealed class McpAuditLogger
{
    private readonly ILogger<McpAuditLogger> _logger;

    public McpAuditLogger(ILogger<McpAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogInvocation(string toolName, string correlationId, bool isSuccess, long elapsedMilliseconds, string? errorMessage = null)
    {
        if (isSuccess)
        {
            _logger.LogInformation(
                "[McpAudit] Tool '{ToolName}' executed successfully in {ElapsedMs}ms (CorrelationId: {CorrelationId})",
                toolName, elapsedMilliseconds, correlationId);
        }
        else
        {
            _logger.LogWarning(
                "[McpAudit] Tool '{ToolName}' failed after {ElapsedMs}ms: {Error} (CorrelationId: {CorrelationId})",
                toolName, elapsedMilliseconds, errorMessage ?? "Unknown error", correlationId);
        }
    }
}

