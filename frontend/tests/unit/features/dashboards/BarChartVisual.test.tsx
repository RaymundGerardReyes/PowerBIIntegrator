import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { BarChartVisual } from "@features/dashboards/components/visuals/BarChartVisual";
import type { Visual } from "@entities/visual/types";

describe("BarChartVisual - Role Validation & Measure Parity", () => {
  it("renders valid bar breakdown when bound to category dimension and DAX measure", () => {
    const visual: Visual = {
      name: "RegionalBar",
      visualType: "barChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Sales[Region]", "Sales[TotalRevenue]"]
    };

    render(<BarChartVisual visual={visual} />);
    expect(screen.getByText(/TotalRevenue by Region/i)).toBeInTheDocument();
    expect(screen.getByText(/DAX Aggregation/i)).toBeInTheDocument();
    expect(screen.queryByTestId("barchart-error-RegionalBar")).not.toBeInTheDocument();
  });

  it("renders validation error state when metric slot is bound to raw unaggregated column", () => {
    const visual: Visual = {
      name: "InvalidBar",
      visualType: "barChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Sales[Region]", "Sales[Revenue]"]
    };

    render(<BarChartVisual visual={visual} />);
    expect(screen.getByTestId("barchart-error-InvalidBar")).toBeInTheDocument();
    expect(screen.getByText(/Invalid Chart Measure Binding/i)).toBeInTheDocument();
    expect(screen.getByText(/Raw unaggregated column 'Revenue' cannot be bound/i)).toBeInTheDocument();
  });

  it("renders warning banner when category axis is bound to a DAX measure", () => {
    const visual: Visual = {
      name: "MeasureAsCategoryBar",
      visualType: "barChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Sales[TotalSales]", "Sales[TotalRevenue]"]
    };

    render(<BarChartVisual visual={visual} />);
    expect(screen.getByTestId("barchart-warning-MeasureAsCategoryBar")).toBeInTheDocument();
    expect(screen.getByText(/Dimension Expected: Measure 'TotalSales' bound to Category axis/i)).toBeInTheDocument();
  });

  it("renders vertical column layout when visualType is columnChart", () => {
    const visual: Visual = {
      name: "TierColumn",
      visualType: "columnChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Customers[tier]", "Customers[TotalRows]"]
    };

    render(<BarChartVisual visual={visual} />);
    expect(screen.getByText(/Vertical Column Distribution/i)).toBeInTheDocument();
    expect(screen.queryByTestId("barchart-error-TierColumn")).not.toBeInTheDocument();
  });
});
