using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Features.DataSources.Rules;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;

public class RegisterDataSourceCommandHandler : IRequestHandler<RegisterDataSourceCommand, Result<DataSourceResponse>>
{
    private readonly IDataSourceRepository _repository;
    private readonly IDataSourceSchemaExtractorFactory _extractorFactory;
    private readonly IAnalyticsModelRepository? _modelRepository;

    public RegisterDataSourceCommandHandler(
        IDataSourceRepository repository,
        IDataSourceSchemaExtractorFactory extractorFactory)
        : this(repository, extractorFactory, null)
    {
    }

    public RegisterDataSourceCommandHandler(
        IDataSourceRepository repository,
        IDataSourceSchemaExtractorFactory extractorFactory,
        IAnalyticsModelRepository? modelRepository)
    {
        _repository = repository;
        _extractorFactory = extractorFactory;
        _modelRepository = modelRepository;
    }

    public async Task<Result<DataSourceResponse>> Handle(RegisterDataSourceCommand request, CancellationToken cancellationToken)
    {
        var nameError = DataSourceDomainRule.ValidateName(request.Name);
        if (nameError != null)
        {
            return Result<DataSourceResponse>.Failure(nameError);
        }

        var pathError = DataSourceDomainRule.ValidateConnectionOrPath(request.ConnectionOrPath);
        if (pathError != null)
        {
            return Result<DataSourceResponse>.Failure(pathError);
        }

        var extractor = _extractorFactory.GetExtractor(request.Type);
        IReadOnlyList<ColumnSchema> schema;
        try
        {
            schema = await extractor.ExtractSchemaAsync(request.ConnectionOrPath, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<DataSourceResponse>.Failure($"Failed to extract schema from data source: {ex.Message}");
        }

        var entity = new DataSourceDefinition(request.Name, request.Type, request.ConnectionOrPath);
        entity.SetSchema(schema);

        await _repository.AddAsync(entity, cancellationToken);

        if (_modelRepository != null)
        {
            var model = AnalyticsPlatform.Application.Features.Analytics.Services.AnalyticsModelFactory.CreateFromDataSource(
                entity.Name,
                entity.Schema,
                connectionOrPath: entity.ConnectionOrPath,
                sourceType: entity.Type);
            await _modelRepository.AddAsync(model, cancellationToken);
        }

        return Result<DataSourceResponse>.Success(new DataSourceResponse(
            entity.Id,
            entity.Name,
            entity.Type.ToString().ToLowerInvariant(),
            entity.ConnectionOrPath,
            entity.Schema));
    }
}

