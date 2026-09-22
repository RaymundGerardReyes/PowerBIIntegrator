import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { TableVisual } from "@features/dashboards/components/visuals/TableVisual";
import type { Visual } from "@entities/visual/types";

describe("TableVisual - Tabular Projections & Measure Parity", () => {
  it("renders tabular headers and data rows for column and measure fields", () => {
    const visual: Visual = {
      name: "CustomerTable",
      visualType: "table",
      layout: { x: 0, y: 0, width: 600, height: 400, z: 0, visible: true },
      boundFields: ["Customers[CustomerId]", "Customers[CustomerName]", "Customers[TotalRevenue]"]
    };

    render(<TableVisual visual={visual} />);
    expect(screen.getByText(/Tabular Grid View \(3 Fields\)/i)).toBeInTheDocument();
    expect(screen.getByText("CustomerId")).toBeInTheDocument();
    expect(screen.getByText("CustomerName")).toBeInTheDocument();
    expect(screen.getByText("TotalRevenue")).toBeInTheDocument();
    // [fx] indicator for DAX measure TotalRevenue
    expect(screen.getByText("[fx]")).toBeInTheDocument();
  });

  it("renders default headers when boundFields is empty", () => {
    const visual: Visual = {
      name: "EmptyTable",
      visualType: "table",
      layout: { x: 0, y: 0, width: 600, height: 400, z: 0, visible: true },
      boundFields: []
    };

    render(<TableVisual visual={visual} />);
    expect(screen.getByText("Column1")).toBeInTheDocument();
    expect(screen.getByText("Measure")).toBeInTheDocument();
  });
});
