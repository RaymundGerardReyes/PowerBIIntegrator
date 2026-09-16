using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.Rules;

public static class SensitiveExposureRules
{
    public static Result<bool> IsCloudTransmissionAllowed(
        SensitivityLevel sensitivity,
        bool hasExplicitOptIn,
        bool containsCameraOrCctvEvents)
    {
        // CCTV and camera surveillance events are strictly forbidden from cloud transmission
        if (containsCameraOrCctvEvents)
        {
            return Result<bool>.Success(false);
        }

        // Sensitive and Restricted data cannot be transmitted to external cloud
        if (sensitivity >= SensitivityLevel.Sensitive)
        {
            return Result<bool>.Success(false);
        }

        // Internal data requires explicit user opt-in for cloud processing
        if (sensitivity == SensitivityLevel.Internal && !hasExplicitOptIn)
        {
            return Result<bool>.Success(false);
        }

        return Result<bool>.Success(true);
    }
}
