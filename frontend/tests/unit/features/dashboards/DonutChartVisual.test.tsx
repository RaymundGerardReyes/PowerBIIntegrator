import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { DonutChartVisual } from "@features/dashboards/components/visuals/DonutChartVisual";
import type { Visual } from "@entities/visual/types";

describe("DonutChartVisual - Role Validation & Measure Parity", () => {
  it("renders valid donut segments when bound to category and DAX measure", () => {
    const visual: Visual = {
      name: "GenderDonut",
      visualType: "donutChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Demographics[Gender]", "Demographics[TotalRows]"]
    };

    render(<DonutChartVisual visual={visual} />);
    expect(screen.getByText(/Proportions by Gender/i)).toBeInTheDocument();
    expect(screen.getByText(/Ring Ratio Distribution/i)).toBeInTheDocument();
    expect(screen.queryByTestId("donutchart-error-GenderDonut")).not.toBeInTheDocument();
  });

  it("renders validation error state when slice metric is bound to an unaggregated column", () => {
    const visual: Visual = {
      name: "InvalidDonut",
      visualType: "donutChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Demographics[Gender]", "Demographics[Age]"]
    };

    render(<DonutChartVisual visual={visual} />);
    expect(screen.getByTestId("donutchart-error-InvalidDonut")).toBeInTheDocument();
    expect(screen.getByText(/Invalid Slice Measure Binding/i)).toBeInTheDocument();
    expect(screen.getByText(/Raw unaggregated column 'Age' cannot be bound/i)).toBeInTheDocument();
  });

  it("renders full pie layout when visualType is pieChart", () => {
    const visual: Visual = {
      name: "GenderPie",
      visualType: "pieChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Demographics[Gender]"]
    };

    render(<DonutChartVisual visual={visual} />);
    expect(screen.getByText(/Pie Chart by Gender/i)).toBeInTheDocument();
    expect(screen.getByText(/Full Proportional Slices/i)).toBeInTheDocument();
  });
});
