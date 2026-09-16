using MediatR;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RegisterLlmPolicy;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RunLlmTask;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.StreamLlmChat;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Queries.GetLlmPolicies;

namespace AnalyticsPlatform.Api.Endpoints;

public static class LlmEndpoints
{
    public static void MapLlmEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/llm").WithTags("LLM Orchestration");

        group.MapPost("/tasks", async (RunLlmTaskCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Errors);
        });

        group.MapGet("/policies", async (ISender sender) =>
        {
            var result = await sender.Send(new GetLlmPoliciesQuery());
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Errors);
        });

        group.MapPost("/policies", async (RegisterLlmPolicyCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(result.Errors);
        });

        group.MapPost("/chat/stream", async (StreamLlmChatCommand command, ISender sender, HttpContext httpContext, CancellationToken ct) =>
        {
            var stream = await sender.Send(command, ct);
            httpContext.Response.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";

            await foreach (var token in stream.WithCancellation(ct))
            {
                await httpContext.Response.WriteAsync($"data: {token}\n\n", ct);
                await httpContext.Response.Body.FlushAsync(ct);
            }
            await httpContext.Response.WriteAsync("data: [DONE]\n\n", ct);
            await httpContext.Response.Body.FlushAsync(ct);
        });
    }
}

