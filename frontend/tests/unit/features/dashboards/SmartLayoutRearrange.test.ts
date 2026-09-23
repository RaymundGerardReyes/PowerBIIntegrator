import { describe, it, expect } from "vitest";
import { computeAutoRearrange, useDashboardStore } from "@features/dashboards/model/dashboardSlice";
import type { Visual } from "@entities/visual/types";

describe("Smart Layout Rearrange & Multi-Page Capacity Engine", () => {
  const sampleKpis: Visual[] = [
    {
      name: "Kpi1",
      visualType: "card",
      layout: { x: 50, y: 50, width: 300, height: 160, visible: true },
      boundFields: ["TotalRows"]
    },
    {
      name: "Kpi2",
      visualType: "card",
      layout: { x: 380, y: 50, width: 300, height: 160, visible: true },
      boundFields: ["TotalRevenue"]
    }
  ];

  const sampleCharts: Visual[] = [
    {
      name: "Chart1",
      visualType: "barChart",
      layout: { x: 50, y: 240, width: 500, height: 300, visible: true },
      boundFields: ["Region", "TotalRevenue"]
    },
    {
      name: "Chart2",
      visualType: "donutChart",
      layout: { x: 580, y: 240, width: 500, height: 300, visible: true },
      boundFields: ["Category", "TotalRows"]
    },
    {
      name: "Chart3",
      visualType: "lineChart",
      layout: { x: 50, y: 560, width: 500, height: 300, visible: true },
      boundFields: ["OrderDate", "TotalRevenue"]
    }
  ];

  describe("computeAutoRearrange algorithm", () => {
    it("returns empty array when no visuals are provided", () => {
      const result = computeAutoRearrange([]);
      expect(result).toEqual([]);
    });

    it("places KPI cards in a balanced top row", () => {
      const result = computeAutoRearrange(sampleKpis, 1280, 720);
      expect(result).toHaveLength(2);
      expect(result[0].layout.y).toBe(20);
      expect(result[1].layout.y).toBe(20);
      expect(result[0].layout.x).toBe(20);
      expect(result[1].layout.x).toBeGreaterThan(result[0].layout.x + result[0].layout.width);
      expect(result[0].layout.height).toBe(150);
    });

    it("positions charts in subsequent grid below KPI cards without vertical overlap", () => {
      const allVisuals = [...sampleKpis, ...sampleCharts];
      const result = computeAutoRearrange(allVisuals, 1280, 720);

      expect(result).toHaveLength(5);
      // All charts start below KPI row (y >= 190)
      const charts = result.filter((v) => v.visualType !== "card");
      for (const chart of charts) {
        expect(chart.layout.y).toBeGreaterThanOrEqual(180);
        // All coordinates stay strictly within 1280x720 canvas
        expect(chart.layout.x + chart.layout.width).toBeLessThanOrEqual(1280);
        expect(chart.layout.y + chart.layout.height).toBeLessThanOrEqual(720);
      }
    });

    it("packs 2 charts into side-by-side balanced columns", () => {
      const twoCharts = sampleCharts.slice(0, 2);
      const result = computeAutoRearrange(twoCharts, 1280, 720);

      expect(result).toHaveLength(2);
      expect(result[0].layout.y).toBe(20);
      expect(result[1].layout.y).toBe(20);
      expect(result[0].layout.width).toBe(610);
      expect(result[1].layout.width).toBe(610);
      expect(result[0].layout.x + result[0].layout.width).toBeLessThanOrEqual(result[1].layout.x);
    });
  });

  describe("dashboardStore Multi-Page Operations", () => {
    it("adds a new report page with unique identifier and defaults", () => {
      useDashboardStore.setState({
        current: {
          id: "dash-1",
          name: "Test Dashboard",
          pages: [
            { name: "Overview", canvasWidth: 1280, canvasHeight: 720, visuals: [] }
          ]
        }
      });

      const newPage = useDashboardStore.getState().addPage("Details");
      expect(newPage).toBe("Details");

      const pages = useDashboardStore.getState().current!.pages;
      expect(pages).toHaveLength(2);
      expect(pages[1].name).toBe("Details");
      expect(pages[1].canvasWidth).toBe(1280);
      expect(pages[1].visuals).toEqual([]);
    });

    it("removes a page but preserves at least one page minimum", () => {
      useDashboardStore.setState({
        current: {
          id: "dash-1",
          name: "Test Dashboard",
          pages: [
            { name: "Overview", canvasWidth: 1280, canvasHeight: 720, visuals: [] },
            { name: "Page 2", canvasWidth: 1280, canvasHeight: 720, visuals: [] }
          ]
        }
      });

      useDashboardStore.getState().removePage("Page 2");
      expect(useDashboardStore.getState().current!.pages).toHaveLength(1);

      // Cannot remove the only remaining page
      useDashboardStore.getState().removePage("Overview");
      expect(useDashboardStore.getState().current!.pages).toHaveLength(1);
    });

    it("moves a visual from source page to target page seamlessly", () => {
      useDashboardStore.setState({
        current: {
          id: "dash-1",
          name: "Test Dashboard",
          pages: [
            {
              name: "Overview",
              canvasWidth: 1280,
              canvasHeight: 720,
              visuals: [sampleCharts[0]]
            },
            {
              name: "Details",
              canvasWidth: 1280,
              canvasHeight: 720,
              visuals: []
            }
          ]
        }
      });

      useDashboardStore.getState().moveVisualToPage("Overview", "Details", "Chart1");

      const overview = useDashboardStore.getState().current!.pages.find((p) => p.name === "Overview")!;
      const details = useDashboardStore.getState().current!.pages.find((p) => p.name === "Details")!;

      expect(overview.visuals).toHaveLength(0);
      expect(details.visuals).toHaveLength(1);
      expect(details.visuals[0].name).toBe("Chart1");
      expect(details.visuals[0].layout.x).toBe(20);
      expect(details.visuals[0].layout.y).toBe(20);
    });

    it("rearranges page visuals in place using rearrangePageVisuals", () => {
      useDashboardStore.setState({
        current: {
          id: "dash-1",
          name: "Test Dashboard",
          pages: [
            {
              name: "Overview",
              canvasWidth: 1280,
              canvasHeight: 720,
              visuals: [
                {
                  name: "Kpi1",
                  visualType: "card",
                  layout: { x: 500, y: 400, width: 200, height: 100, visible: true },
                  boundFields: ["TotalRows"]
                }
              ]
            }
          ]
        }
      });

      useDashboardStore.getState().rearrangePageVisuals("Overview");

      const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
      expect(visual.layout.x).toBe(20);
      expect(visual.layout.y).toBe(20);
      expect(visual.layout.height).toBe(150);
    });
  });
});
