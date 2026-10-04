using System.Data;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Infrastructure.Features.DataQuality.Connectors;

public class TabularBatchReader
{
    public TabularBatch ReadFromDataTable(string sourceName, DataTable table)
    {
        var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var rows = new List<TabularRow>();

        int idx = 0;
        foreach (DataRow dr in table.Rows)
        {
            var fields = new Dictionary<string, string?>();
            foreach (var col in columns)
            {
                var val = dr[col];
                fields[col] = val == DBNull.Value ? null : val?.ToString();
            }
            rows.Add(new TabularRow($"row_{idx++}", fields));
        }

        return new TabularBatch(sourceName, columns, rows);
    }
}

