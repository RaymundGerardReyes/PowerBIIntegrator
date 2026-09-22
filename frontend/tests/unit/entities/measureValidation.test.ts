import { describe, it, expect } from "vitest";
import { isValidMeasureName, cleanFieldLabel, isDimensionCandidate, validateVisualRoles } from "@entities/measure";

describe("Measure Validation Utilities", () => {
  describe("cleanFieldLabel", () => {
    it("strips table name wrapper brackets correctly", () => {
      expect(cleanFieldLabel("Sales[TotalRevenue]")).toBe("TotalRevenue");
      expect(cleanFieldLabel("FactOrders[Revenue]")).toBe("Revenue");
      expect(cleanFieldLabel("titanic[target_Rate]")).toBe("target_Rate");
      expect(cleanFieldLabel("Model[TotalRows]")).toBe("TotalRows");
    });

    it("handles plain field names without table brackets", () => {
      expect(cleanFieldLabel("TotalRevenue")).toBe("TotalRevenue");
      expect(cleanFieldLabel("CustomerName")).toBe("CustomerName");
      expect(cleanFieldLabel("")).toBe("");
    });
  });

  describe("isValidMeasureName", () => {
    it("accepts valid measure names starting with Total, Sum, Average, Avg", () => {
      expect(isValidMeasureName("TotalRevenue")).toBe(true);
      expect(isValidMeasureName("Total_Sales")).toBe(true);
      expect(isValidMeasureName("Sales[Total_Revenue]")).toBe(true);
      expect(isValidMeasureName("Sum_Quantity")).toBe(true);
      expect(isValidMeasureName("SumAmount")).toBe(true);
      expect(isValidMeasureName("AveragePrice")).toBe(true);
      expect(isValidMeasureName("Average_Fare")).toBe(true);
      expect(isValidMeasureName("AvgDiscount")).toBe(true);
    });

    it("accepts valid default TotalRows count measures", () => {
      expect(isValidMeasureName("TotalRows")).toBe(true);
      expect(isValidMeasureName("totalrows")).toBe(true);
      expect(isValidMeasureName("Sales[TotalRows]")).toBe(true);
      expect(isValidMeasureName("total_rows")).toBe(true);
    });

    it("accepts valid measures ending with _Rate, Rate, or _Pct", () => {
      expect(isValidMeasureName("target_Rate")).toBe(true);
      expect(isValidMeasureName("churn_rate")).toBe(true);
      expect(isValidMeasureName("SurvivalRate")).toBe(true);
      expect(isValidMeasureName("growth_pct")).toBe(true);
      expect(isValidMeasureName("margin_percentage")).toBe(true);
    });

    it("rejects raw unaggregated columns", () => {
      expect(isValidMeasureName("CustomerName")).toBe(false);
      expect(isValidMeasureName("OrderId")).toBe(false);
      expect(isValidMeasureName("Revenue")).toBe(false);
      expect(isValidMeasureName("Amount")).toBe(false);
      expect(isValidMeasureName("Price")).toBe(false);
      expect(isValidMeasureName("Age")).toBe(false);
      expect(isValidMeasureName("ProductCategory")).toBe(false);
      expect(isValidMeasureName("Sales[Revenue]")).toBe(false);
    });
  });

  describe("isDimensionCandidate", () => {
    it("identifies raw column names as dimension candidates", () => {
      expect(isDimensionCandidate("Category")).toBe(true);
      expect(isDimensionCandidate("Region")).toBe(true);
      expect(isDimensionCandidate("CustomerName")).toBe(true);
      expect(isDimensionCandidate("OrderDate")).toBe(true);
    });

    it("rejects DAX measures as dimensions", () => {
      expect(isDimensionCandidate("TotalRevenue")).toBe(false);
      expect(isDimensionCandidate("SumQuantity")).toBe(false);
      expect(isDimensionCandidate("TotalRows")).toBe(false);
    });
  });

  describe("validateVisualRoles", () => {
    it("validates card visuals requiring valid DAX measures", () => {
      expect(validateVisualRoles("card", ["Sales[TotalRevenue]"]).isValid).toBe(true);
      const invalid = validateVisualRoles("card", ["Sales[Revenue]"]);
      expect(invalid.isValid).toBe(false);
      expect(invalid.error).toContain("Raw unaggregated column 'Revenue' cannot be used in a KPI Card");
    });

    it("validates bar chart visuals with category and measure slots", () => {
      const valid = validateVisualRoles("barChart", ["Sales[Region]", "Sales[TotalRevenue]"]);
      expect(valid.isValid).toBe(true);
      expect(valid.warning).toBeUndefined();

      const invalidMeasure = validateVisualRoles("barChart", ["Sales[Region]", "Sales[Revenue]"]);
      expect(invalidMeasure.isValid).toBe(false);
      expect(invalidMeasure.error).toContain("cannot be bound to the Value (Y) axis");

      const measureAsCategory = validateVisualRoles("barChart", ["Sales[TotalSales]", "Sales[TotalRevenue]"]);
      expect(measureAsCategory.isValid).toBe(true);
      expect(measureAsCategory.warning).toContain("Dimension Expected");
    });

    it("validates line chart visuals requiring measures for Y-axis", () => {
      expect(validateVisualRoles("lineChart", ["Orders[OrderDate]", "Orders[TotalSales]"]).isValid).toBe(true);
      const invalid = validateVisualRoles("lineChart", ["Orders[OrderDate]", "Orders[Amount]"]);
      expect(invalid.isValid).toBe(false);
      expect(invalid.error).toContain("cannot be bound to the Y axis");
    });

    it("validates donut chart visuals requiring measures for slice values", () => {
      expect(validateVisualRoles("donutChart", ["Data[Category]", "Data[TotalRows]"]).isValid).toBe(true);
      const invalid = validateVisualRoles("donutChart", ["Data[Category]", "Data[CountRaw]"]);
      expect(invalid.isValid).toBe(false);
      expect(invalid.error).toContain("cannot be bound to the slice value");
    });

    it("accepts table visuals with any column or measure combination", () => {
      expect(validateVisualRoles("table", ["Data[Id]", "Data[Name]", "Data[TotalSales]"]).isValid).toBe(true);
    });
  });
});
