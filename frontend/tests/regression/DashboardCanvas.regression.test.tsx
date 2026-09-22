import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { DashboardCanvas } from "@features/dashboards/components/DashboardCanvas";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";

describe("Regression: DashboardCanvas markup snapshot", () => {
  it("matches the last known-good render", () => {
    useDashboardStore.getState().setDashboard({
      id: "d1",
      name: "Snapshot",
      pages: [{ name: "P1", canvasWidth: 800, canvasHeight: 600, visuals: [] }]
    });
    const client = new QueryClient();
    const { container } = render(
      <QueryClientProvider client={client}>
        <DashboardCanvas />
      </QueryClientProvider>
    );
    expect(container.innerHTML).toMatchSnapshot();
  });

  it("renders a populated dashboard with all core visual types stably", () => {
    useDashboardStore.getState().setDashboard({
      id: "d-populated",
      name: "Executive Performance",
      pages: [
        {
          name: "Overview",
          canvasWidth: 1280,
          canvasHeight: 720,
          visuals: [
            {
              name: "TotalRevenueKpi",
              visualType: "card",
              layout: { x: 40, y: 30, width: 340, height: 160, z: 1, visible: true },
              boundFields: ["Sales[TotalRevenue]"]
            },
            {
              name: "RegionalSalesBar",
              visualType: "barChart",
              layout: { x: 40, y: 210, width: 440, height: 320, z: 1, visible: true },
              boundFields: ["Sales[Region]", "Sales[TotalRevenue]"]
            },
            {
              name: "SalesTrendLine",
              visualType: "lineChart",
              layout: { x: 500, y: 210, width: 440, height: 320, z: 1, visible: true },
              boundFields: ["Orders[OrderDate]", "Orders[TotalSales]"]
            },
            {
              name: "GenderDonut",
              visualType: "donutChart",
              layout: { x: 960, y: 210, width: 280, height: 320, z: 1, visible: true },
              boundFields: ["Customers[Gender]", "Customers[TotalRows]"]
            },
            {
              name: "RecordsTable",
              visualType: "table",
              layout: { x: 40, y: 550, width: 1200, height: 150, z: 1, visible: true },
              boundFields: ["Customers[Id]", "Customers[Name]", "Customers[TotalRevenue]"]
            }
          ]
        }
      ]
    });

    const client = new QueryClient();
    const { container } = render(
      <QueryClientProvider client={client}>
        <DashboardCanvas />
      </QueryClientProvider>
    );

    expect(container.querySelector("[data-testid=\"visual-TotalRevenueKpi\"]")).toBeInTheDocument();
    expect(container.querySelector("[data-testid=\"visual-RegionalSalesBar\"]")).toBeInTheDocument();
    expect(container.querySelector("[data-testid=\"visual-SalesTrendLine\"]")).toBeInTheDocument();
    expect(container.querySelector("[data-testid=\"visual-GenderDonut\"]")).toBeInTheDocument();
    expect(container.querySelector("[data-testid=\"visual-RecordsTable\"]")).toBeInTheDocument();
  });
});
