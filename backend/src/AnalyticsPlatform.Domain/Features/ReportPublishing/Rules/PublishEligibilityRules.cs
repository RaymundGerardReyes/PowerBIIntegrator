namespace AnalyticsPlatform.Domain.Features.ReportPublishing.Rules;

public static class PublishEligibilityRules
{
    public static bool IsEligible(int pageCount, int visualCount)
        => pageCount > 0 && visualCount > 0 && visualCount <= 40;
}
