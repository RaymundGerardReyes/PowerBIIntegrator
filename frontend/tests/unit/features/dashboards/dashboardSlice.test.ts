import { describe, it, expect, beforeEach } from "vitest";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";

describe("dashboardSlice", () => {
  beforeEach(() => {
    useDashboardStore.setState({ current: null });
  });

  it("updates a visual's layout by page and visual name", () => {
    useDashboardStore.getState().setDashboard({
      id: "d1",
      name: "Test",
      pages: [
        {
          name: "Page1",
          canvasWidth: 1920,
          canvasHeight: 1080,
          visuals: [{ name: "kpi1", visualType: "card", layout: { x: 0, y: 0, width: 100, height: 100, visible: true }, boundFields: [] }]
        }
      ]
    });

    useDashboardStore.getState().updateVisualLayout("Page1", "kpi1", { x: 50 });

    expect(useDashboardStore.getState().current?.pages[0].visuals[0].layout.x).toBe(50);
  });
});
