using AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;
using AnalyticsPlatform.Domain.Features.AiAdvisory.Rules;
using AnalyticsPlatform.Domain.Features.AiAdvisory.ValueObjects;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Domain.AiAdvisory;

public class ExposureDecisionRulesTests
{
    [Fact]
    public void RestrictedData_IsAlwaysBlocked_RegardlessOfPolicyOrRole()
    {
        // Arrange
        var elevatedPolicy = AdvisoryPolicy.DataStewardElevated();

        // Act - Even with elevated policy and explicit human unlock, Restricted MUST be blocked
        var decision = ExposureDecisionRules.EvaluateExposure(
            SensitivityLevel.Restricted,
            "CustomerSocialSecurityNumber",
            elevatedPolicy,
            hasExplicitHumanUnlock: true);

        // Assert
        Assert.Equal(ExposureDecisionType.BlockedRestricted, decision.Decision);
        Assert.Equal("[BLOCKED: RESTRICTED]", decision.MaskedPlaceholder);
    }

    [Fact]
    public void SensitivityExceedingPolicyMax_IsRedacted()
    {
        // Arrange: StrictLocal policy has MaxExposureLevel = Public
        var strictPolicy = AdvisoryPolicy.StrictLocal();

        // Act: Internal field against Public ceiling
        var decision = ExposureDecisionRules.EvaluateExposure(
            SensitivityLevel.Internal,
            "DatabaseTableName",
            strictPolicy,
            hasExplicitHumanUnlock: false);

        // Assert
        Assert.Equal(ExposureDecisionType.Redacted, decision.Decision);
        Assert.Equal("[REDACTED: DatabaseTableName]", decision.MaskedPlaceholder);
    }

    [Fact]
    public void SensitiveData_WithoutExplicitUnlock_IsHeldPendingApproval()
    {
        // Arrange: Default policy requires human approval for sensitive
        var defaultPolicy = AdvisoryPolicy.Default();

        // Act: Sensitive field without unlock
        var decision = ExposureDecisionRules.EvaluateExposure(
            SensitivityLevel.Sensitive,
            "CustomerSampleRow",
            defaultPolicy,
            hasExplicitHumanUnlock: false);

        // Assert
        Assert.Equal(ExposureDecisionType.HeldPendingApproval, decision.Decision);
        Assert.Equal("[PENDING_APPROVAL: CustomerSampleRow]", decision.MaskedPlaceholder);
    }

    [Fact]
    public void SensitiveData_WithExplicitUnlock_IsIncluded()
    {
        // Arrange: DataSteward policy with unlock
        var elevatedPolicy = AdvisoryPolicy.DataStewardElevated();

        // Act: Sensitive field with unlock
        var decision = ExposureDecisionRules.EvaluateExposure(
            SensitivityLevel.Sensitive,
            "CustomerSampleRow",
            elevatedPolicy,
            hasExplicitHumanUnlock: true);

        // Assert
        Assert.Equal(ExposureDecisionType.IncludeAsIs, decision.Decision);
    }

    [Fact]
    public void PublicData_WithinThreshold_IsIncludedAsIs()
    {
        // Arrange
        var defaultPolicy = AdvisoryPolicy.Default();

        // Act
        var decision = ExposureDecisionRules.EvaluateExposure(
            SensitivityLevel.Public,
            "RowCount",
            defaultPolicy);

        // Assert
        Assert.Equal(ExposureDecisionType.IncludeAsIs, decision.Decision);
    }
}

