import { describe, it, expect } from "vitest";
import { apiClient } from "@shared/lib/http/apiClient";

describe("Analytics API integration", () => {
  it("creates a measure via the mocked backend contract", async () => {
    const response = await apiClient.post("/api/analytics/measures", {
      name: "TotalRevenue",
      expression: "SUM(Sales[Amount])",
      tableName: "Sales"
    });
    expect(response.status).toBe(200);
    expect(response.data.id).toBe("measure-1");
  });
});
