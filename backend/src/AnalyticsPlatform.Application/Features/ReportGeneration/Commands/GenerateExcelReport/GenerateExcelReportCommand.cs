using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateExcelReport;

public sealed record GenerateExcelReportCommand(ReportDocumentModel Model) : IRequest<Result<GeneratedReportDto>>;
