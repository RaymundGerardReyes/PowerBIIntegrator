using System.Text.Json.Serialization;

namespace AnalyticsPlatform.McpServer.Protocol;

public sealed record JsonRpcResponse(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("id")] object? Id,
    [property: JsonPropertyName("result")] object? Result,
    [property: JsonPropertyName("error")] JsonRpcError? Error
)
{
    public static JsonRpcResponse Success(object? id, object? result) =>
        new("2.0", id, result, null);

    public static JsonRpcResponse FromError(object? id, int code, string message, object? data = null) =>
        new("2.0", id, null, new JsonRpcError(code, message, data));
}
