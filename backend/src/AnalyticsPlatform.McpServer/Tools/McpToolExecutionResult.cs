using System.Text.Json.Serialization;

namespace AnalyticsPlatform.McpServer.Tools;

public sealed record McpToolContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text
);

public sealed record McpToolExecutionResult(
    [property: JsonPropertyName("content")] IReadOnlyList<McpToolContent> Content,
    [property: JsonPropertyName("isError")] bool IsError
)
{
    private static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    public static McpToolExecutionResult Success(object payload) =>
        new(new[] { new McpToolContent("text", System.Text.Json.JsonSerializer.Serialize(payload, SerializerOptions)) }, false);

    public static McpToolExecutionResult SuccessText(string text) =>
        new(new[] { new McpToolContent("text", text) }, false);

    public static McpToolExecutionResult Failed(string errorText) =>
        new(new[] { new McpToolContent("text", errorText) }, true);
}
