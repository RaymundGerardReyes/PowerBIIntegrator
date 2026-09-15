using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;

public sealed record DataSourceResponse(
    Guid Id,
    string Name,
    string Type,
    string ConnectionOrPath,
    IReadOnlyList<ColumnSchema> Schema);

public sealed record RegisterDataSourceCommand(
    string Name,
    DataSourceType Type,
    string ConnectionOrPath) : IRequest<Result<DataSourceResponse>>;
