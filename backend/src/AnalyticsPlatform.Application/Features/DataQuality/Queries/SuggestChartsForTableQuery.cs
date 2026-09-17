using MediatR;
using AnalyticsPlatform.Application.Features.DataQuality.Commands;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataQuality.Rules;

namespace AnalyticsPlatform.Application.Features.DataQuality.Queries;

public record SuggestChartsForTableQuery(string TableId) : IRequest<Result<IReadOnlyList<ChartSuggestion>>>;

public class SuggestChartsForTableQueryHandler : IRequestHandler<SuggestChartsForTableQuery, Result<IReadOnlyList<ChartSuggestion>>>
{
    private readonly ISender _sender;

    public SuggestChartsForTableQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Result<IReadOnlyList<ChartSuggestion>>> Handle(SuggestChartsForTableQuery request, CancellationToken cancellationToken)
    {
        var profileResult = await _sender.Send(new ProfileDatasetCommand(request.TableId, request.TableId), cancellationToken);
        if (profileResult.IsSuccess && profileResult.Value != null && profileResult.Value.ColumnProfiles.Count > 0)
        {
            var suggestions = VisualMappingRule.MapSuggestions(profileResult.Value);
            return Result.Success(suggestions);
        }

        var fallbackProfile = new DatasetProfile(request.TableId, request.TableId, 0);
        return Result.Success(VisualMappingRule.MapSuggestions(fallbackProfile));
    }
}

