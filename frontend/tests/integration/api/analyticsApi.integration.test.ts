import { describe, it, expect } from "vitest";
import * as analyticsApi from "@features/analytics/api/analyticsApi";

describe("Analytics Model API integration", () => {
  it("validates semantic model integrity via the mocked backend contract", async () => {
    const result = await analyticsApi.validateAnalyticsModel("model-123");

    expect(result.isValid).toBe(true);
    expect(result.modelName).toBe("SalesModel");
    expect(result.errors).toEqual([]);
    expect(result.detectedCycles).toEqual([]);
  });

  it("creates a measure via analyticsApi", async () => {
    const result = await analyticsApi.createMeasure({
      name: "TotalProfit",
      expression: "SUM(Sales[Profit])",
      tableName: "Sales"
    });

    expect(result.id).toBe("measure-1");
  });
});

