using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GeneratePdfReport;

public sealed record GeneratePdfReportCommand(ReportDocumentModel Model) : IRequest<Result<GeneratedReportDto>>;
