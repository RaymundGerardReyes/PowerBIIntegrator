using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using AnalyticsPlatform.McpServer.Protocol;

namespace AnalyticsPlatform.McpServer.Hosting;

public static class SseMcpServerHost
{
    private static readonly ConcurrentDictionary<string, HttpResponse> ActiveSessions = new();

    public static void MapMcpSseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/mcp").WithTags("Model Context Protocol");

        group.MapGet("/sse", async (HttpContext context, CancellationToken ct) =>
        {
            var sessionId = Guid.NewGuid().ToString();
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("X-Accel-Buffering", "no");

            ActiveSessions[sessionId] = context.Response;

            // Send initial endpoint event per MCP specification
            await context.Response.WriteAsync($"event: endpoint\ndata: /mcp/message?sessionId={sessionId}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(15000, ct);
                    await context.Response.WriteAsync("event: ping\ndata: {}\n\n", ct);
                    await context.Response.Body.FlushAsync(ct);
                }
            }
            finally
            {
                ActiveSessions.TryRemove(sessionId, out _);
            }
        });

        group.MapPost("/message", async (HttpContext context, StdioMcpServerHost host, CancellationToken ct) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            if (string.IsNullOrWhiteSpace(body))
            {
                return Results.BadRequest(JsonRpcResponse.FromError(null, JsonRpcError.InvalidRequest, "Empty request body"));
            }

            var request = JsonSerializer.Deserialize<JsonRpcRequest>(body);
            if (request == null)
            {
                return Results.BadRequest(JsonRpcResponse.FromError(null, JsonRpcError.InvalidRequest, "Invalid JSON-RPC"));
            }

            var response = await host.HandleRequestAsync(request, ct);
            return Results.Ok(response);
        });
    }
}
