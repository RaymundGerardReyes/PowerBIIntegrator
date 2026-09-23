import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { LineChartVisual } from "@features/dashboards/components/visuals/LineChartVisual";
import type { Visual } from "@entities/visual/types";

describe("LineChartVisual - Role Validation, Basis Accuracy & Measure Parity", () => {
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
    expect(screen.getByTestId("chart-basis-line")).toBeInTheDocument();
    expect(screen.getByTestId("chart-y-axis")).toBeInTheDocument();
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
    expect(screen.getByTestId("chart-basis-line")).toBeInTheDocument();
    expect(screen.queryByTestId("linechart-error-AreaTrend")).not.toBeInTheDocument();
  });

  it("intelligently orients a single bound DAX measure as Y metric over Timeline", () => {
    const visual: Visual = {
      name: "SingleMeasureTrend",
      visualType: "lineChart",
      layout: { x: 0, y: 0, width: 400, height: 300, z: 0, visible: true },
      boundFields: ["titanic[TotalRows]"]
    };

    render(<LineChartVisual visual={visual} />);
    // Should NOT render backwards "Metric by TotalRows"
    expect(screen.queryByText(/Metric by TotalRows/i)).not.toBeInTheDocument();
    expect(screen.getByText(/TotalRows over Timeline/i)).toBeInTheDocument();
    expect(screen.getByTestId("chart-basis-line")).toBeInTheDocument();
    expect(screen.getByTestId("chart-y-axis")).toBeInTheDocument();
  });
});
