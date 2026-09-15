using MediatR;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.Analytics.Commands.CreateMeasure;

public sealed record CreateMeasureResponse(Guid Id, string Name, string Expression, string TableName);

public sealed record CreateMeasureCommand(string Name, string Expression, string TableName) : IRequest<Result<CreateMeasureResponse>>;
