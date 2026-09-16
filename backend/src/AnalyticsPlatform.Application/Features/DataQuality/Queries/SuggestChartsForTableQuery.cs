using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataQuality.Rules;

namespace AnalyticsPlatform.Application.Features.DataQuality.Queries;

public record SuggestChartsForTableQuery(string TableId) : IRequest<Result<IReadOnlyList<ChartSuggestion>>>;

public class SuggestChartsForTableQueryHandler : IRequestHandler<SuggestChartsForTableQuery, Result<IReadOnlyList<ChartSuggestion>>>
{
    public Task<Result<IReadOnlyList<ChartSuggestion>>> Handle(SuggestChartsForTableQuery request, CancellationToken cancellationToken)
    {
        var dummyProfile = new DatasetProfile(request.TableId, request.TableId, 100);
        dummyProfile.AddColumnProfile(new ColumnProfile("OrderDate", "DateTime", 100, 0, 0.0, 95, "2026-01-01", "2026-09-16", new[] { "2026-09-16" }, @"^\d{4}-\d{2}-\d{2}$", "High"));
        dummyProfile.AddColumnProfile(new ColumnProfile("Revenue", "Decimal", 100, 0, 0.0, 90, "100.00", "50000.00", new[] { "500.00" }, @"^\d+\.\d+$", "High"));
        dummyProfile.AddColumnProfile(new ColumnProfile("Region", "String", 100, 0, 0.0, 4, "APAC", "US", new[] { "APAC", "EU" }, @"^[A-Z]+$", "Low"));

        var suggestions = VisualMappingRule.MapSuggestions(dummyProfile);
        return Task.FromResult(Result.Success(suggestions));
    }
}

