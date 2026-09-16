using System.Text.Json.Serialization;

namespace AnalyticsPlatform.McpServer.Protocol;

public sealed record McpServerInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version
);

public sealed record McpServerCapabilities(
    [property: JsonPropertyName("tools")] object? Tools = null,
    [property: JsonPropertyName("resources")] object? Resources = null,
    [property: JsonPropertyName("prompts")] object? Prompts = null,
    [property: JsonPropertyName("logging")] object? Logging = null
);

public sealed record McpInitializeResult(
    [property: JsonPropertyName("protocolVersion")] string ProtocolVersion,
    [property: JsonPropertyName("capabilities")] McpServerCapabilities Capabilities,
    [property: JsonPropertyName("serverInfo")] McpServerInfo ServerInfo
);
