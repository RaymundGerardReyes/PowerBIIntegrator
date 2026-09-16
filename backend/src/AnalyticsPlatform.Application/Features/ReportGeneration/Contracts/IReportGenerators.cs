using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;

public interface IPdfReportGenerator
{
    Task<byte[]> GenerateAsync(ReportDocumentModel model, CancellationToken cancellationToken = default);
}

public interface IExcelReportGenerator
{
    Task<byte[]> GenerateAsync(ReportDocumentModel model, CancellationToken cancellationToken = default);
}

public interface IWordReportGenerator
{
    Task<byte[]> GenerateAsync(ReportDocumentModel model, CancellationToken cancellationToken = default);
}
