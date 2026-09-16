using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateWordReport;

public sealed record GenerateWordReportCommand(ReportDocumentModel Model) : IRequest<Result<GeneratedReportDto>>;
