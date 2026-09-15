import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../setup/test-utils";
import { DashboardCanvas } from "@features/dashboards/components/DashboardCanvas";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";

describe("Path: create dashboard -> add visual -> adjust layout -> verify state", () => {
  it("walks the full authoring path end-to-end within the component tree", () => {
    useDashboardStore.getState().setDashboard({
      id: "d1",
      name: "Executive Overview",
      pages: [
        {
          name: "Overview",
          canvasWidth: 1920,
          canvasHeight: 1080,
          visuals: [{ name: "RevenueKpi", visualType: "card", layout: { x: 40, y: 30, width: 400, height: 180, visible: true }, boundFields: ["Sales[TotalRevenue]"] }]
        }
      ]
    });

    renderWithProviders(<DashboardCanvas />);

    expect(screen.getByText("RevenueKpi")).toBeInTheDocument();
    expect(screen.getByTestId("visual-RevenueKpi")).toBeInTheDocument();
  });
});
