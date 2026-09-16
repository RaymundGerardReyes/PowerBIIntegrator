using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Commands;

public record ProfileDatasetCommand(string SourceReference, string DatasetName) : IRequest<Result<DatasetProfile>>;

public class ProfileDatasetCommandHandler : IRequestHandler<ProfileDatasetCommand, Result<DatasetProfile>>
{
    public Task<Result<DatasetProfile>> Handle(ProfileDatasetCommand request, CancellationToken cancellationToken)
    {
        var profile = new DatasetProfile(request.DatasetName, request.SourceReference, 100);
        profile.AddColumnProfile(new ColumnProfile("Id", "Int64", 100, 0, 0.0, 100, "1", "100", new[] { "1", "2" }, @"^\d+$", "High"));
        profile.AddColumnProfile(new ColumnProfile("CreatedDate", "DateTime", 100, 2, 0.02, 98, "2026-01-01", "2026-09-16", new[] { "2026-09-16" }, @"^\d{4}-\d{2}-\d{2}$", "High"));
        profile.AddColumnProfile(new ColumnProfile("Category", "String", 100, 0, 0.0, 4, "A", "D", new[] { "A", "B" }, @"^[A-Z]+$", "Low"));
        profile.AddColumnProfile(new ColumnProfile("Amount", "Decimal", 100, 1, 0.01, 85, "10.50", "9999.00", new[] { "100.00" }, @"^\d+\.\d+$", "High"));

        return Task.FromResult(Result.Success(profile));
    }
}

