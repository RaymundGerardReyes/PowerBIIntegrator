using MediatR;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Commands.UnlockConfidentialExposure;

public sealed record UnlockConfidentialExposureCommand(
    UnlockConfidentialRequest Request
) : IRequest<Result<UnlockConfidentialResultDto>>;

public sealed class UnlockConfidentialExposureCommandHandler : IRequestHandler<UnlockConfidentialExposureCommand, Result<UnlockConfidentialResultDto>>
{
    private readonly IAdvisoryAuditLogger _auditLogger;

    public UnlockConfidentialExposureCommandHandler(IAdvisoryAuditLogger auditLogger)
    {
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    public async Task<Result<UnlockConfidentialResultDto>> Handle(UnlockConfidentialExposureCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        // Verify DataSteward role
        if (!string.Equals(req.UserRole, "DataSteward", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(req.UserRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Result<UnlockConfidentialResultDto>.Failure("Unauthorized: Only users with 'DataSteward' or higher role can unlock confidential sample row exposure.");
        }

        if (string.IsNullOrWhiteSpace(req.Reason))
        {
            return Result<UnlockConfidentialResultDto>.Failure("A business justification reason must be provided for unlocking confidential exposure.");
        }

        // Audited single-use unlock token
        var token = $"unlock-{Guid.NewGuid():N}";
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        await _auditLogger.LogConfidentialUnlockAsync(req.RunId, req.UserId, req.Reason, cancellationToken);

        var result = new UnlockConfidentialResultDto(
            Success: true,
            Token: token,
            Message: $"Confidential row samples unlocked for Run '{req.RunId}' by {req.UserId}. Provider locked to LocalOllama.",
            ExpiresAtUtc: expiresAt);

        return Result<UnlockConfidentialResultDto>.Success(result);
    }
}

