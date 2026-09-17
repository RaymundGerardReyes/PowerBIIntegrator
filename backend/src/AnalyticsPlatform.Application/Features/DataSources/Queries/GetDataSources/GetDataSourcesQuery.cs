using MediatR;
using AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.DataSources.Queries.GetDataSources;

public sealed record GetDataSourcesQuery : IRequest<Result<IReadOnlyList<DataSourceResponse>>>;

public class GetDataSourcesQueryHandler : IRequestHandler<GetDataSourcesQuery, Result<IReadOnlyList<DataSourceResponse>>>
{
    private readonly IDataSourceRepository _repository;

    public GetDataSourcesQueryHandler(IDataSourceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<DataSourceResponse>>> Handle(GetDataSourcesQuery request, CancellationToken cancellationToken)
    {
        var dataSources = await _repository.GetAllAsync(cancellationToken);
        var responses = dataSources.Select(ds => new DataSourceResponse(
            ds.Id,
            ds.Name,
            ds.Type.ToString(),
            ds.ConnectionOrPath,
            ds.Schema
        )).ToList();

        return Result<IReadOnlyList<DataSourceResponse>>.Success(responses);
    }
}

