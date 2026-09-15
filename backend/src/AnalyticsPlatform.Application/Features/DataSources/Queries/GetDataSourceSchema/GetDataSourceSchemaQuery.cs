using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.DataSources.Queries.GetDataSourceSchema;

public sealed record GetDataSourceSchemaQuery(Guid DataSourceId) : IRequest<Result<IReadOnlyList<ColumnSchema>>>;

public class GetDataSourceSchemaQueryHandler : IRequestHandler<GetDataSourceSchemaQuery, Result<IReadOnlyList<ColumnSchema>>>
{
    private readonly IDataSourceRepository _repository;

    public GetDataSourceSchemaQueryHandler(IDataSourceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ColumnSchema>>> Handle(GetDataSourceSchemaQuery request, CancellationToken cancellationToken)
    {
        var dataSource = await _repository.GetByIdAsync(request.DataSourceId, cancellationToken);
        if (dataSource == null)
        {
            return Result<IReadOnlyList<ColumnSchema>>.Failure($"Data source with ID '{request.DataSourceId}' was not found.");
        }

        return Result<IReadOnlyList<ColumnSchema>>.Success(dataSource.Schema);
    }
}

