using MediatR;
using AnalyticsPlatform.Application.Features.Analytics.Queries.GetAnalyticsModel;
using AnalyticsPlatform.Application.Features.Analytics.Services;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.Analytics.Queries.GetAnalyticsModels;

public sealed record GetAnalyticsModelsQuery : IRequest<Result<IReadOnlyList<AnalyticsModelDto>>>;

public class GetAnalyticsModelsQueryHandler : IRequestHandler<GetAnalyticsModelsQuery, Result<IReadOnlyList<AnalyticsModelDto>>>
{
    private readonly IAnalyticsModelRepository _modelRepository;
    private readonly IDataSourceRepository _dataSourceRepository;

    public GetAnalyticsModelsQueryHandler(
        IAnalyticsModelRepository modelRepository,
        IDataSourceRepository dataSourceRepository)
    {
        _modelRepository = modelRepository;
        _dataSourceRepository = dataSourceRepository;
    }

    public async Task<Result<IReadOnlyList<AnalyticsModelDto>>> Handle(GetAnalyticsModelsQuery request, CancellationToken cancellationToken)
    {
        // 1. Check all registered data sources and ensure a corresponding model exists
        var dataSources = await _dataSourceRepository.GetAllAsync(cancellationToken);
        var existingModels = await _modelRepository.GetAllAsync(cancellationToken);

        foreach (var ds in dataSources)
        {
            var expectedModelId = AnalyticsModelFactory.GenerateDeterministicGuid(ds.Name);
            if (!existingModels.Any(m => m.Id == expectedModelId || m.Name.StartsWith(ds.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var generatedModel = AnalyticsModelFactory.CreateFromDataSource(ds.Name, ds.Schema, expectedModelId);
                await _modelRepository.AddAsync(generatedModel, cancellationToken);
            }
        }

        // 2. Fetch updated models list
        var allModels = await _modelRepository.GetAllAsync(cancellationToken);

        var dtos = allModels.Select(model => new AnalyticsModelDto(
            model.Id,
            model.Name,
            model.Culture,
            model.Tables.Select(t => t.Name).ToList(),
            model.Measures.Select(m => m.Name).ToList(),
            model.Relationships.Count
        )).ToList();

        return Result<IReadOnlyList<AnalyticsModelDto>>.Success(dtos);
    }
}

