import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { CardVisual } from "@features/dashboards/components/visuals/CardVisual";
import type { Visual } from "@entities/visual/types";

describe("CardVisual - Semantic Measure Parity", () => {
  it("renders KPI value for valid measures (e.g. Total_Revenue)", () => {
    const visual: Visual = {
      name: "TotalRevenueCard",
      visualType: "card",
      layout: { x: 0, y: 0, width: 300, height: 200, z: 0, visible: true },
      boundFields: ["Sales[Total_Revenue]"]
    };

    render(<CardVisual visual={visual} />);
    expect(screen.getByText(/DAX Validated/i)).toBeInTheDocument();
    expect(screen.getByText(/Total Revenue/i)).toBeInTheDocument();
    expect(screen.queryByText(/Invalid Measure Binding/i)).not.toBeInTheDocument();
  });

  it("renders KPI value for default TotalRows measure", () => {
    const visual: Visual = {
      name: "TotalRowsCard",
      visualType: "card",
      layout: { x: 0, y: 0, width: 300, height: 200, z: 0, visible: true },
      boundFields: ["Sales[TotalRows]"]
    };

    render(<CardVisual visual={visual} />);
    expect(screen.getByText(/DAX Validated/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Total Records/i).length).toBeGreaterThan(0);
    expect(screen.queryByText(/Invalid Measure Binding/i)).not.toBeInTheDocument();
  });

  it("renders error state when bound to raw unaggregated column (e.g. CustomerName)", () => {
    const visual: Visual = {
      name: "CustomerCard",
      visualType: "card",
      layout: { x: 0, y: 0, width: 300, height: 200, z: 0, visible: true },
      boundFields: ["Customer[CustomerName]"]
    };

    render(<CardVisual visual={visual} />);
    expect(screen.getByText(/Invalid Measure Binding/i)).toBeInTheDocument();
    expect(screen.getByText(/Raw unaggregated column/i)).toBeInTheDocument();
    expect(screen.getByTestId("card-error-CustomerCard")).toBeInTheDocument();
  });

  it("renders error state when bound to raw unaggregated numeric column (e.g. Revenue instead of Total_Revenue)", () => {
    const visual: Visual = {
      name: "RawRevenueCard",
      visualType: "card",
      layout: { x: 0, y: 0, width: 300, height: 200, z: 0, visible: true },
      boundFields: ["Sales[Revenue]"]
    };

    render(<CardVisual visual={visual} />);
    expect(screen.getByText(/Invalid Measure Binding/i)).toBeInTheDocument();
    expect(screen.getByText(/Raw unaggregated column/i)).toBeInTheDocument();
  });
});

