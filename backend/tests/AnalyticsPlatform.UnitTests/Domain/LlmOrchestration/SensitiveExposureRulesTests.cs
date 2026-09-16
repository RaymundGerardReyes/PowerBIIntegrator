using FluentAssertions;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Rules;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Domain.LlmOrchestration;

public class SensitiveExposureRulesTests
{
    [Fact]
    public void IsCloudTransmissionAllowed_WhenContainsCameraOrCctvEvents_ReturnsFalse()
    {
        var result = SensitiveExposureRules.IsCloudTransmissionAllowed(
            SensitivityLevel.Public,
            hasExplicitOptIn: true,
            containsCameraOrCctvEvents: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void IsCloudTransmissionAllowed_WhenSensitivityIsSensitive_ReturnsFalse()
    {
        var result = SensitiveExposureRules.IsCloudTransmissionAllowed(
            SensitivityLevel.Sensitive,
            hasExplicitOptIn: true,
            containsCameraOrCctvEvents: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void IsCloudTransmissionAllowed_WhenSensitivityIsRestricted_ReturnsFalse()
    {
        var result = SensitiveExposureRules.IsCloudTransmissionAllowed(
            SensitivityLevel.Restricted,
            hasExplicitOptIn: true,
            containsCameraOrCctvEvents: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void IsCloudTransmissionAllowed_WhenInternalWithoutExplicitOptIn_ReturnsFalse()
    {
        var result = SensitiveExposureRules.IsCloudTransmissionAllowed(
            SensitivityLevel.Internal,
            hasExplicitOptIn: false,
            containsCameraOrCctvEvents: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void IsCloudTransmissionAllowed_WhenInternalWithExplicitOptIn_ReturnsTrue()
    {
        var result = SensitiveExposureRules.IsCloudTransmissionAllowed(
            SensitivityLevel.Internal,
            hasExplicitOptIn: true,
            containsCameraOrCctvEvents: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void IsCloudTransmissionAllowed_WhenPublicWithoutCctv_ReturnsTrue()
    {
        var result = SensitiveExposureRules.IsCloudTransmissionAllowed(
            SensitivityLevel.Public,
            hasExplicitOptIn: false,
            containsCameraOrCctvEvents: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}

