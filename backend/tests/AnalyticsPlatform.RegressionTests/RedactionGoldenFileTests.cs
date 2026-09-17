using System.Text.RegularExpressions;
using FluentAssertions;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;
using AnalyticsPlatform.Domain.Features.AiAdvisory.Rules;
using AnalyticsPlatform.Domain.Features.AiAdvisory.ValueObjects;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Repositories;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;
using Xunit;

namespace AnalyticsPlatform.RegressionTests;

public sealed partial class RedactionGoldenFileTests
{
    [GeneratedRegex("^[a-z0-9_]+$")]
    private static partial Regex ToolNameRegex();

    [Fact]
    public void AllSixAdvisoryTools_ConformToNamingConvention()
    {
        var repo = new InMemoryAdvisoryRunRepository();
        var tools = new IAdvisoryTool[]
        {
            new GetPipelineRunResultTool(repo),
            new GetDatasetProfileTool(repo),
            new GetDuplicateClustersTool(repo),
            new GetSchemaViolationsTool(repo),
            new GetTransformationPlanTool(repo),
            new GetChartSuggestionsTool(repo)
        };

        foreach (var tool in tools)
        {
            tool.Name.Should().NotBeNullOrWhiteSpace();
            tool.Description.Should().NotBeNullOrWhiteSpace();
            ToolNameRegex().IsMatch(tool.Name).Should().BeTrue($"Tool name '{tool.Name}' must be lowercase alphanumeric with underscores.");
        }
    }

    [Fact]
    public void RedactionDecision_ProducesExactGoldenMaskedPlaceholder()
    {
        var policy = AdvisoryPolicy.StrictLocal(); // Max = Public

        var internalDecision = ExposureDecisionRules.EvaluateExposure(
            SensitivityLevel.Internal,
            "InternalTableMetadata",
            policy);

        var sensitiveDecision = ExposureDecisionRules.EvaluateExposure(
            SensitivityLevel.Sensitive,
            "SampleCustomerRow",
            AdvisoryPolicy.Default());

        var restrictedDecision = ExposureDecisionRules.EvaluateExposure(
            SensitivityLevel.Restricted,
            "RawPiiCredentials",
            AdvisoryPolicy.DataStewardElevated());

        // Golden contract assertions
        internalDecision.MaskedPlaceholder.Should().Be("[REDACTED: InternalTableMetadata]");
        sensitiveDecision.MaskedPlaceholder.Should().Be("[PENDING_APPROVAL: SampleCustomerRow]");
        restrictedDecision.MaskedPlaceholder.Should().Be("[BLOCKED: RESTRICTED]");
    }
}

