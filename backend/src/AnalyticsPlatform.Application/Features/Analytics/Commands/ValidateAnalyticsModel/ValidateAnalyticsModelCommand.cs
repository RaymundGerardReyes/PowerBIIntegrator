using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Analytics.Rules;

namespace AnalyticsPlatform.Application.Features.Analytics.Commands.ValidateAnalyticsModel;

public sealed record ValidateAnalyticsModelResponse(
    Guid ModelId,
    string ModelName,
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> OrphanTables,
    IReadOnlyList<string> DetectedCycles);

public sealed record ValidateAnalyticsModelCommand(Guid AnalyticsModelId) : IRequest<Result<ValidateAnalyticsModelResponse>>;

