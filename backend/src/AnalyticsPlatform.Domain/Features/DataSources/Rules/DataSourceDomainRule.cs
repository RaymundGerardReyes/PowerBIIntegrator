namespace AnalyticsPlatform.Domain.Features.DataSources.Rules;

/// <summary>Domain-level validation rules for data source registration.</summary>
public static class DataSourceDomainRule
{
    /// <summary>
    /// Returns an error message if the data source name violates domain rules, or null if valid.
    /// </summary>
    public static string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Data source name must not be empty.";
        if (name.Length > 200)
            return "Data source name must not exceed 200 characters.";
        return null;
    }

    /// <summary>
    /// Returns an error message if the connection string / file path violates domain rules, or null if valid.
    /// </summary>
    public static string? ValidateConnectionOrPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "ConnectionOrPath must not be empty.";
        return null;
    }
}

