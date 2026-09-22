import { describe, it, expect, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { VisualLayoutEditor } from "@features/dashboards/components/VisualLayoutEditor";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";
import type { Visual } from "@entities/visual/types";

describe("VisualLayoutEditor - Guardrails & Role Grouping", () => {
  beforeEach(() => {
    useDashboardStore.getState().setDashboard({
      id: "d-test",
      name: "Test Dashboard",
      pages: [
        {
          name: "Page1",
          canvasWidth: 1280,
          canvasHeight: 720,
          visuals: [
            {
              name: "SalesBar",
              visualType: "barChart",
              layout: { x: 40, y: 30, width: 400, height: 300, z: 1, visible: true },
              boundFields: ["Sales[Region]", "Sales[TotalRevenue]"]
            },
            {
              name: "RevenueKpi",
              visualType: "card",
              layout: { x: 480, y: 30, width: 300, height: 180, z: 1, visible: true },
              boundFields: ["Sales[TotalRevenue]"]
            }
          ]
        }
      ]
    });
  });

  it("renders visual container and header controls", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Page1" visual={visual} />);

    expect(screen.getByTestId("visual-SalesBar")).toBeInTheDocument();
    expect(screen.getByText("SalesBar")).toBeInTheDocument();
    expect(screen.getByLabelText("select-type-SalesBar")).toBeInTheDocument();
  });

  it("renders both category and metric dropdowns for 2-slot bar chart", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Page1" visual={visual} />);

    expect(screen.getByLabelText("select-field-SalesBar")).toBeInTheDocument();
    expect(screen.getByLabelText("select-measure-SalesBar")).toBeInTheDocument();
  });

  it("groups fields into DAX Measures and Raw Columns for KPI cards", () => {
    const cardVisual = useDashboardStore.getState().current!.pages[0].visuals[1];
    render(<VisualLayoutEditor pageName="Page1" visual={cardVisual} />);

    const fieldSelect = screen.getByLabelText("select-field-RevenueKpi");
    expect(fieldSelect).toBeInTheDocument();
    expect(fieldSelect.innerHTML).toContain("DAX Measures (Valid)");
  });

  it("nudges visual layout coordinates when clicking move button", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Page1" visual={visual} />);

    const moveBtn = screen.getByLabelText("move-SalesBar");
    fireEvent.click(moveBtn);

    const updatedVisual = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updatedVisual.layout.x).toBe(50); // 40 + 10
  });
});
