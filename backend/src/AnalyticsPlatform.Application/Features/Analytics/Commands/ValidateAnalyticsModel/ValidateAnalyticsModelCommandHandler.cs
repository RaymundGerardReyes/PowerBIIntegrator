using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Analytics.Rules;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.Analytics.Commands.ValidateAnalyticsModel;

public class ValidateAnalyticsModelCommandHandler : IRequestHandler<ValidateAnalyticsModelCommand, Result<ValidateAnalyticsModelResponse>>
{
    private readonly IAnalyticsModelRepository _repository;

    public ValidateAnalyticsModelCommandHandler(IAnalyticsModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ValidateAnalyticsModelResponse>> Handle(ValidateAnalyticsModelCommand request, CancellationToken cancellationToken)
    {
        var model = await _repository.GetByIdAsync(request.AnalyticsModelId, cancellationToken);
        if (model == null)
        {
            return Result<ValidateAnalyticsModelResponse>.Failure($"Analytics model with ID '{request.AnalyticsModelId}' was not found.");
        }

        var report = ValidateAnalyticsModelRules.Validate(model);

        var response = new ValidateAnalyticsModelResponse(
            model.Id,
            model.Name,
            report.IsValid,
            report.Errors,
            report.Warnings,
            report.OrphanTables,
            report.DetectedCycles);

        return Result<ValidateAnalyticsModelResponse>.Success(response);
    }
}

