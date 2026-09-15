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
});
