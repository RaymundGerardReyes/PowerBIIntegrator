using Microsoft.Extensions.Logging;

namespace AnalyticsPlatform.McpServer.Security;

public class ToolPermissionMiddleware
{
    private static readonly HashSet<string> PrivilegedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "publish_pbip_to_fabric"
    };

    private static readonly HashSet<string> SensitiveTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "query_event_summary"
    };

    private readonly ILogger<ToolPermissionMiddleware> _logger;

    public bool IsPrivilegedCaller { get; set; } = true;
    public bool AllowSensitive { get; set; } = true;

    public ToolPermissionMiddleware(ILogger<ToolPermissionMiddleware> logger)
    {
        _logger = logger;
    }

    public virtual bool AuthorizeToolInvocation(string toolName, string correlationId)
    {
        return AuthorizeToolInvocation(toolName, correlationId, IsPrivilegedCaller, AllowSensitive);
    }

    public virtual bool AuthorizeToolInvocation(string toolName, string correlationId, bool isPrivilegedCaller, bool allowSensitive)
    {
        if (PrivilegedTools.Contains(toolName) && !isPrivilegedCaller)
        {
            _logger.LogWarning(
                "[McpSecurity] Unauthorized privileged tool invocation blocked: {ToolName} (CorrelationId: {CorrelationId})",
                toolName, correlationId);
            return false;
        }

        if (SensitiveTools.Contains(toolName) && !allowSensitive)
        {
            _logger.LogWarning(
                "[McpSecurity] Sensitive tool invocation blocked by security policy: {ToolName} (CorrelationId: {CorrelationId})",
                toolName, correlationId);
            return false;
        }

        return true;
    }
}

