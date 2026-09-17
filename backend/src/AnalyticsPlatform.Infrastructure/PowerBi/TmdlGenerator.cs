using System.Text;
using System.Text.Json;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class TmdlGenerator : ITmdlGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IVirtualFileTree GenerateSemanticModel(AnalyticsModel model)
    {
        var fileTree = new VirtualFileTree();

        // 1. definition.pbism
        var pbism = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/semanticModel/definitionProperties/1.0.0/schema.json",
            ["version"] = "4.0",
            ["settings"] = new { }
        };
        fileTree.AddTextFile("definition.pbism", JsonSerializer.Serialize(pbism, JsonOptions));

        // 2. definition/model.tmdl
        var modelSb = new StringBuilder();
        modelSb.AppendLine("model Model");
        modelSb.AppendLine($"\tculture: {model.Culture}");
        modelSb.AppendLine("\tdefaultPowerBIDataSourceVersion: powerBI_V3");
        modelSb.AppendLine();

        // Ensure any standalone measures in model without a table are assigned to a Default table
        var tables = model.Tables.ToList();
        if (tables.Count == 0 || model.Measures.Any(m => !tables.Any(t => t.Measures.Any(tm => tm.Name == m.Name))))
        {
            var defaultTable = tables.FirstOrDefault(t => t.Name == "ModelMeasures");
            if (defaultTable == null)
            {
                defaultTable = new ModelTable("ModelMeasures");
                defaultTable.AddColumn(new ModelColumn("RowId", ColumnDataType.Int64, "RowId"));
                tables.Add(defaultTable);
            }

            foreach (var measure in model.Measures)
            {
                if (!defaultTable.Measures.Any(m => m.Name == measure.Name))
                {
                    defaultTable.AddMeasure(measure);
                }
            }
        }

        foreach (var table in tables)
        {
            modelSb.AppendLine($"ref table '{table.Name}'");
        }
        fileTree.AddTextFile("definition/model.tmdl", modelSb.ToString());

        // 3. definition/relationships.tmdl
        if (model.Relationships.Count > 0)
        {
            var relSb = new StringBuilder();
            foreach (var rel in model.Relationships)
            {
                relSb.AppendLine($"relationship '{rel.Name}'");
                relSb.AppendLine($"\tfromColumn: '{rel.FromTable}'.'{rel.FromColumn}'");
                relSb.AppendLine($"\ttoColumn: '{rel.ToTable}'.'{rel.ToColumn}'");
                if (rel.CrossFiltering == RelationshipCrossFiltering.Both)
                {
                    relSb.AppendLine("\tcrossFilteringBehavior: bothDirections");
                }
                if (!rel.IsActive)
                {
                    relSb.AppendLine("\tisActive: false");
                }
                relSb.AppendLine();
            }
            fileTree.AddTextFile("definition/relationships.tmdl", relSb.ToString());
        }

        // 4. definition/tables/<table_name>.tmdl
        foreach (var table in tables)
        {
            var tableTmdl = BuildTableTmdl(table);
            fileTree.AddTextFile($"definition/tables/{table.Name}.tmdl", tableTmdl);
        }

        return fileTree;
    }

    private static string BuildTableTmdl(ModelTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"table '{table.Name}'");
        sb.AppendLine($"\tlineageTag: {table.LineageTag}");
        sb.AppendLine();

        // Columns
        foreach (var col in table.Columns)
        {
            sb.AppendLine($"\tcolumn '{col.Name}'");
            sb.AppendLine($"\t\tdataType: {FormatDataType(col.DataType)}");
            if (!string.IsNullOrEmpty(col.FormatString))
            {
                sb.AppendLine($"\t\tformatString: {col.FormatString}");
            }
            sb.AppendLine($"\t\tsourceColumn: {col.SourceColumn}");
            sb.AppendLine();
        }

        // Measures
        foreach (var measure in table.Measures)
        {
            sb.AppendLine($"\tmeasure '{measure.Name}' = {measure.Expression.DaxOrFormula}");
            sb.AppendLine("\t\tformatString: #,0.00");
            sb.AppendLine();
        }

        // Partition (Dual-mode: M Query if supplied, or schema-only partition)
        var partitionName = table.PartitionName ?? $"{table.Name}-Partition";
        sb.AppendLine($"\tpartition '{partitionName}' = m");
        sb.AppendLine("\t\tmode: import");
        sb.AppendLine("\t\tsource =");

        if (!string.IsNullOrWhiteSpace(table.MQueryPartition))
        {
            foreach (var line in table.MQueryPartition.Split('\n'))
            {
                sb.AppendLine($"\t\t\t{line.TrimEnd('\r')}");
            }
        }
        else
        {
            sb.AppendLine("\t\t\tlet");
            sb.AppendLine("\t\t\t    Source = #table(type table [], {})");
            sb.AppendLine("\t\t\tin");
            sb.AppendLine("\t\t\t    Source");
        }

        return sb.ToString();
    }

    private static string FormatDataType(ColumnDataType dataType) => dataType switch
    {
        ColumnDataType.Int64 => "int64",
        ColumnDataType.Decimal => "decimal",
        ColumnDataType.DateTime => "dateTime",
        ColumnDataType.Boolean => "boolean",
        _ => "string"
    };

    public string GenerateMeasureTmdl(Measure measure)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"measure '{measure.Name}' = {measure.Expression.DaxOrFormula}");
        sb.AppendLine("\tformatString: #,0.00");
        return sb.ToString();
    }
}
