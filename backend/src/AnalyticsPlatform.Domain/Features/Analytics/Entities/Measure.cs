using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;

namespace AnalyticsPlatform.Domain.Features.Analytics.Entities;

public class Measure : Entity
{
    public string Name { get; private set; }
    public MeasureExpression Expression { get; private set; }
    public string TableName { get; private set; }

    private Measure(string name, MeasureExpression expression, string tableName)
    {
        Name = name;
        Expression = expression;
        TableName = tableName;
    }

    public static Result<Measure> Create(string name, MeasureExpression expression, string tableName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Measure>.Failure("Measure name cannot be empty.");

        if (string.IsNullOrWhiteSpace(expression.DaxOrFormula))
            return Result<Measure>.Failure("Measure expression cannot be empty.");

        if (string.IsNullOrWhiteSpace(tableName))
            return Result<Measure>.Failure("Table name cannot be empty.");

        return Result<Measure>.Success(new Measure(name, expression, tableName));
    }
}
