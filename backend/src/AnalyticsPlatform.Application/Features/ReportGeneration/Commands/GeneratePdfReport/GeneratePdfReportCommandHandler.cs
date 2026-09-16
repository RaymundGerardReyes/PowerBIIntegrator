using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GeneratePdfReport;

public sealed class GeneratePdfReportCommandHandler : IRequestHandler<GeneratePdfReportCommand, Result<GeneratedReportDto>>
{
    private readonly IPdfReportGenerator _pdfReportGenerator;

    public GeneratePdfReportCommandHandler(IPdfReportGenerator pdfReportGenerator)
    {
        _pdfReportGenerator = pdfReportGenerator;
    }

    public async Task<Result<GeneratedReportDto>> Handle(GeneratePdfReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _pdfReportGenerator.GenerateAsync(request.Model, cancellationToken);
            var safeTitle = string.Join("_", request.Model.Title.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
            var fileName = $"{safeTitle}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.pdf";

            var dto = new GeneratedReportDto(
                content,
                "application/pdf",
                fileName);

            return Result<GeneratedReportDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<GeneratedReportDto>.Failure($"PDF report generation failed: {ex.Message}");
        }
    }
}
