using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;

namespace AnalyticsPlatform.Application.Features.Analytics.Commands.CreateMeasure;

public class CreateMeasureCommandHandler : IRequestHandler<CreateMeasureCommand, Result<CreateMeasureResponse>>
{
    public Task<Result<CreateMeasureResponse>> Handle(CreateMeasureCommand request, CancellationToken cancellationToken)
    {
        var expression = new MeasureExpression(request.Expression, "decimal");
        var result = Measure.Create(request.Name, expression, request.TableName);

        if (!result.IsSuccess || result.Value is null)
            return Task.FromResult(Result<CreateMeasureResponse>.Failure(result.Errors));

        var response = new CreateMeasureResponse(result.Value.Id, result.Value.Name, request.Expression, request.TableName);
        return Task.FromResult(Result<CreateMeasureResponse>.Success(response));
    }
}
