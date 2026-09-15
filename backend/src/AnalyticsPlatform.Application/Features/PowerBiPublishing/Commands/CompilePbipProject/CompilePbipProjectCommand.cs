using MediatR;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbipProject;

public sealed record PbipFileEntryDto(string RelativePath, int SizeInBytes);

public sealed record CompilePbipResponse(
    string ProjectName,
    int TotalFiles,
    IReadOnlyList<PbipFileEntryDto> Files,
    IReadOnlyDictionary<string, string> Manifest);

public sealed record CompilePbipProjectCommand(
    Guid DashboardDefinitionId,
    Guid AnalyticsModelId,
    string? ProjectName = null) : IRequest<Result<CompilePbipResponse>>;

