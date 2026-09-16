using MediatR;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateExcelReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GeneratePdfReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateWordReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reports").WithTags("ReportGeneration");

        group.MapPost("/pdf", async (ReportDocumentModel model, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GeneratePdfReportCommand(model), ct);
            return result.IsSuccess && result.Value != null
                ? Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
                : Results.BadRequest(result.Errors);
        })
        .WithName("GeneratePdfReport")
        .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/excel", async (ReportDocumentModel model, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GenerateExcelReportCommand(model), ct);
            return result.IsSuccess && result.Value != null
                ? Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
                : Results.BadRequest(result.Errors);
        })
        .WithName("GenerateExcelReport")
        .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/word", async (ReportDocumentModel model, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GenerateWordReportCommand(model), ct);
            return result.IsSuccess && result.Value != null
                ? Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
                : Results.BadRequest(result.Errors);
        })
        .WithName("GenerateWordReport")
        .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
