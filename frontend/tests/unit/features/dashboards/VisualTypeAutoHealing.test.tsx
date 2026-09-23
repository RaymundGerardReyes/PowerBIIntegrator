import { describe, it, expect, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { VisualLayoutEditor } from "@features/dashboards/components/VisualLayoutEditor";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";
import type { DashboardDefinition } from "@features/dashboards/model/types";

describe("VisualLayoutEditor - Visual Type Auto-Healing & Metric Computation", () => {
  const initialDashboard: DashboardDefinition = {
    id: "dash-healing-1",
    name: "Auto-Healing Dashboard",
    pages: [
      {
        name: "Overview",
        canvasWidth: 1280,
        canvasHeight: 720,
        visuals: [
          {
            name: "VisualA",
            visualType: "columnChart",
            layout: { x: 40, y: 40, width: 440, height: 320, z: 1, visible: true },
            boundFields: ["titanic[pclass]", "titanic[target_Rate]"]
          }
        ]
      }
    ]
  };

  beforeEach(() => {
    useDashboardStore.setState({ current: JSON.parse(JSON.stringify(initialDashboard)) });
  });

  it("auto-heals boundFields to valid DAX measure when switching from columnChart to card", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    // Initial state is columnChart with pclass and target_Rate
    const typeSelect = screen.getByLabelText("select-type-VisualA");
    expect(typeSelect).toHaveValue("columnChart");

    // Switch visual type to KPI Card
    fireEvent.change(typeSelect, { target: { value: "card" } });

    // The boundFields should have auto-healed to [titanic[target_Rate]]
    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updated.visualType).toBe("card");
    expect(updated.boundFields).toEqual(["titanic[target_Rate]"]);
    // Must NOT contain unaggregated column pclass
    expect(updated.boundFields).not.toContain("titanic[pclass]");
  });

  it("auto-heals boundFields to [dimension, measure] when switching from card to columnChart", () => {
    // Set initial visual as a KPI Card with TotalRows
    useDashboardStore.setState((state) => ({
      current: {
        ...state.current!,
        pages: [
          {
            ...state.current!.pages[0],
            visuals: [
              {
                name: "CardVisual1",
                visualType: "card",
                layout: { x: 40, y: 40, width: 300, height: 160, z: 1, visible: true },
                boundFields: ["titanic[TotalRows]"]
              }
            ]
          }
        ]
      }
    }));

    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    const typeSelect = screen.getByLabelText("select-type-CardVisual1");
    // Switch to columnChart
    fireEvent.change(typeSelect, { target: { value: "columnChart" } });

    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updated.visualType).toBe("columnChart");
    // Slot 0 must be a dimension, Slot 1 must be the DAX measure TotalRows
    expect(updated.boundFields[0]).toBe("titanic[pclass]");
    expect(updated.boundFields[1]).toBe("titanic[TotalRows]");
  });

  it("auto-swaps field to Slot 1 if user selects a DAX measure in Slot 0 of a multi-axis chart", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    const slot0Select = screen.getByLabelText("select-field-VisualA");
    // Select measure in slot 0
    fireEvent.change(slot0Select, { target: { value: "titanic[TotalRows]" } });

    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    // Slot 0 should retain or get a dimension, and Slot 1 should get the measure TotalRows
    expect(updated.boundFields[1]).toBe("titanic[TotalRows]");
    expect(updated.boundFields[0]).toBe("titanic[pclass]");
  });

  it("renders verified computed rates on BarChartVisual when bound to target_Rate", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    // Verify true calculated survival rates are displayed: 1st Class (62.0%), 2nd Class (47.3%), 3rd Class (24.2%)
    expect(screen.getByText("62.0%")).toBeInTheDocument();
    expect(screen.getByText("47.3%")).toBeInTheDocument();
    expect(screen.getByText("24.2%")).toBeInTheDocument();
    expect(screen.getByText(/target Rate by Pclass/i)).toBeInTheDocument();
    expect(screen.queryByText(/Dimension Expected/i)).not.toBeInTheDocument();
  });

  it("maintains valid role parity when transitioning across all 8 visual types in succession", () => {
    const types = ["barChart", "lineChart", "areaChart", "donutChart", "pieChart", "card", "table", "columnChart"];
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    const { rerender } = render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    const typeSelect = screen.getByLabelText("select-type-VisualA");

    for (const t of types) {
      fireEvent.change(typeSelect, { target: { value: t } });
      const current = useDashboardStore.getState().current!.pages[0].visuals[0];
      expect(current.visualType).toBe(t);

      if (t === "card") {
        expect(current.boundFields.length).toBe(1);
        expect(current.boundFields[0]).toMatch(/Rate|Total/);
      } else if (t !== "table") {
        expect(current.boundFields.length).toBeGreaterThanOrEqual(2);
        // Slot 0 must not be a measure
        expect(current.boundFields[0]).not.toMatch(/Rate|Total/);
        // Slot 1 must be a measure
        expect(current.boundFields[1]).toMatch(/Rate|Total/);
      }

      rerender(
        <VisualLayoutEditor
          pageName="Overview"
          visual={current}
          canvasWidth={1280}
          canvasHeight={720}
        />
      );

      // Verify no invalid binding errors rendered in the DOM
      expect(screen.queryByText(/Invalid Measure Binding/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/Dimension Expected/i)).not.toBeInTheDocument();
    }
  });
});

