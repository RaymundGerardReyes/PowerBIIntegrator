using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Infrastructure.Repositories;

public class AnalyticsModelRepository : IAnalyticsModelRepository
{
    private static readonly Guid DefaultModelId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Dictionary<Guid, AnalyticsModel> _store = new();

    public AnalyticsModelRepository()
    {
        SeedDefaultModel();
    }

    private void SeedDefaultModel()
    {
        var model = new AnalyticsModel(DefaultModelId, "Enterprise Sales & Finance Semantic Model");

        var salesTable = model.GetOrAddTable("Sales");
        salesTable.AddColumn(new ModelColumn("TotalRevenue", ColumnDataType.Decimal, "TotalRevenue"));
        salesTable.AddColumn(new ModelColumn("OperatingMargin", ColumnDataType.Decimal, "OperatingMargin"));
        salesTable.AddColumn(new ModelColumn("CustomerId", ColumnDataType.Int64, "CustomerId"));
        salesTable.AddColumn(new ModelColumn("DateKey", ColumnDataType.DateTime, "DateKey"));
        salesTable.AddColumn(new ModelColumn("GeographyId", ColumnDataType.Int64, "GeographyId"));

        var customersTable = model.GetOrAddTable("Customers");
        customersTable.AddColumn(new ModelColumn("Id", ColumnDataType.Int64, "Id"));
        customersTable.AddColumn(new ModelColumn("ActiveCount", ColumnDataType.Int64, "ActiveCount"));

        var dateTable = model.GetOrAddTable("Date");
        dateTable.AddColumn(new ModelColumn("DateKey", ColumnDataType.DateTime, "DateKey"));
        dateTable.AddColumn(new ModelColumn("Month", ColumnDataType.String, "Month"));

        var geoTable = model.GetOrAddTable("Geography");
        geoTable.AddColumn(new ModelColumn("Id", ColumnDataType.Int64, "Id"));
        geoTable.AddColumn(new ModelColumn("Region", ColumnDataType.String, "Region"));
        geoTable.AddColumn(new ModelColumn("Territory", ColumnDataType.String, "Territory"));

        model.AddRelationship(new ModelRelationship("Sales_Customers", "Sales", "CustomerId", "Customers", "Id"));
        model.AddRelationship(new ModelRelationship("Sales_Date", "Sales", "DateKey", "Date", "DateKey"));
        model.AddRelationship(new ModelRelationship("Sales_Geography", "Sales", "GeographyId", "Geography", "Id"));

        _store[DefaultModelId] = model;
    }

    public Task<AnalyticsModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var model);
        return Task.FromResult(model);
    }

    public Task AddAsync(AnalyticsModel model, CancellationToken ct = default)
    {
        _store[model.Id] = model;
        return Task.CompletedTask;
    }
}
