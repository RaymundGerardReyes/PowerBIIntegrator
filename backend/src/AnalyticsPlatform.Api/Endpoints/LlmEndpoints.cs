using MediatR;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RegisterLlmPolicy;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RunLlmTask;
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
    }
}
