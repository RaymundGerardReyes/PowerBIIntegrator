using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateWordReport;

public sealed class GenerateWordReportCommandHandler : IRequestHandler<GenerateWordReportCommand, Result<GeneratedReportDto>>
{
    private readonly IWordReportGenerator _wordReportGenerator;

    public GenerateWordReportCommandHandler(IWordReportGenerator wordReportGenerator)
    {
        _wordReportGenerator = wordReportGenerator;
    }

    public async Task<Result<GeneratedReportDto>> Handle(GenerateWordReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _wordReportGenerator.GenerateAsync(request.Model, cancellationToken);
            var safeTitle = string.Join("_", request.Model.Title.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
            var fileName = $"{safeTitle}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.docx";

            var dto = new GeneratedReportDto(
                content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);

            return Result<GeneratedReportDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<GeneratedReportDto>.Failure($"Word report generation failed: {ex.Message}");
        }
    }
}
