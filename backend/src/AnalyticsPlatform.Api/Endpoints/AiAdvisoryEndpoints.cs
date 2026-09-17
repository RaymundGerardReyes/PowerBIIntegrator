using MediatR;
using AnalyticsPlatform.Application.Features.AiAdvisory.Commands.RunAdvisoryQuery;
using AnalyticsPlatform.Application.Features.AiAdvisory.Commands.UnlockConfidentialExposure;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Queries.GetAdvisoryPolicies;

namespace AnalyticsPlatform.Api.Endpoints;

public static class AiAdvisoryEndpoints
{
    public static void MapAiAdvisoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/advisory").WithTags("AiAdvisory");

        group.MapPost("/query", async (AdvisoryQueryRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RunAdvisoryQueryCommand(request));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapGet("/policies", async (ISender sender) =>
        {
            var result = await sender.Send(new GetAdvisoryPoliciesQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapPost("/unlock", async (UnlockConfidentialRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UnlockConfidentialExposureCommand(request));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });
    }
}

