using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateExcelReport;

public sealed class GenerateExcelReportCommandHandler : IRequestHandler<GenerateExcelReportCommand, Result<GeneratedReportDto>>
{
    private readonly IExcelReportGenerator _excelReportGenerator;

    public GenerateExcelReportCommandHandler(IExcelReportGenerator excelReportGenerator)
    {
        _excelReportGenerator = excelReportGenerator;
    }

    public async Task<Result<GeneratedReportDto>> Handle(GenerateExcelReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _excelReportGenerator.GenerateAsync(request.Model, cancellationToken);
            var safeTitle = string.Join("_", request.Model.Title.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
            var fileName = $"{safeTitle}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.xlsx";

            var dto = new GeneratedReportDto(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);

            return Result<GeneratedReportDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<GeneratedReportDto>.Failure($"Excel report generation failed: {ex.Message}");
        }
    }
}
