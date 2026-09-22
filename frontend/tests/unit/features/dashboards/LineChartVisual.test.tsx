import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { LineChartVisual } from "@features/dashboards/components/visuals/LineChartVisual";
import type { Visual } from "@entities/visual/types";

describe("LineChartVisual - Role Validation & Measure Parity", () => {
  it("renders valid line chart when bound to timeline dimension and DAX measure", () => {
    const visual: Visual = {
      name: "TrendLine",
      visualType: "lineChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Orders[OrderDate]", "Orders[TotalSales]"]
    };

    render(<LineChartVisual visual={visual} />);
    expect(screen.getByText(/TotalSales by OrderDate/i)).toBeInTheDocument();
    expect(screen.getByText(/Continuous Line Trend/i)).toBeInTheDocument();
    expect(screen.queryByTestId("linechart-error-TrendLine")).not.toBeInTheDocument();
  });

  it("renders validation error state when Y-axis is bound to an unaggregated raw column", () => {
    const visual: Visual = {
      name: "InvalidLine",
      visualType: "lineChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Orders[OrderDate]", "Orders[Amount]"]
    };

    render(<LineChartVisual visual={visual} />);
    expect(screen.getByTestId("linechart-error-InvalidLine")).toBeInTheDocument();
    expect(screen.getByText(/Invalid Trendline Measure Binding/i)).toBeInTheDocument();
    expect(screen.getByText(/Raw unaggregated column 'Amount' cannot be bound to the Y axis/i)).toBeInTheDocument();
  });

  it("renders area chart layout subtitle when visualType is areaChart", () => {
    const visual: Visual = {
      name: "AreaTrend",
      visualType: "areaChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["Orders[OrderDate]", "Orders[TotalRows]"]
    };

    render(<LineChartVisual visual={visual} />);
    expect(screen.getByText(/Area Distribution Trendline/i)).toBeInTheDocument();
    expect(screen.queryByTestId("linechart-error-AreaTrend")).not.toBeInTheDocument();
  });
});
