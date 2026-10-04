import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { SlicerVisual } from "@features/dashboards/components/visuals/SlicerVisual";
import type { Visual } from "@entities/visual/types";

describe("SlicerVisual - Interactive Dimension Filtering & Parity Guardrails", () => {
  it("renders dimension items for Pclass dimension (e.g. Pclass 1, Pclass 2, Pclass 3)", () => {
    const visual: Visual = {
      name: "PclassSlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["titanic[pclass]"]
    };

    render(<SlicerVisual visual={visual} />);

    expect(screen.getByTestId("slicer-PclassSlicer")).toBeInTheDocument();
    expect(screen.getByText("pclass")).toBeInTheDocument();
    expect(screen.getByText("Pclass 1")).toBeInTheDocument();
    expect(screen.getByText("Pclass 2")).toBeInTheDocument();
    expect(screen.getByText("Pclass 3")).toBeInTheDocument();
    expect(screen.getByTestId("slicer-item-1")).toBeInTheDocument();
    expect(screen.getByTestId("slicer-item-2")).toBeInTheDocument();
    expect(screen.getByTestId("slicer-item-3")).toBeInTheDocument();
  });

  it("renders dimension items for Sex/Gender dimension (Male, Female)", () => {
    const visual: Visual = {
      name: "SexSlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["titanic[sex]"]
    };

    render(<SlicerVisual visual={visual} />);

    expect(screen.getByText("Male")).toBeInTheDocument();
    expect(screen.getByText("Female")).toBeInTheDocument();
  });

  it("allows interactive toggling of dimension items", () => {
    const onSelectionChange = vi.fn();
    const visual: Visual = {
      name: "PclassSlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["titanic[pclass]"]
    };

    render(<SlicerVisual visual={visual} onSelectionChange={onSelectionChange} />);

    // Initially all 3 are selected (summary says "All")
    expect(screen.getByTestId("slicer-selection-summary")).toHaveTextContent("All");

    // Click item 1 to toggle it off
    const item1 = screen.getByTestId("slicer-item-1");
    fireEvent.click(item1);

    expect(onSelectionChange).toHaveBeenCalledWith(["2", "3"]);
    expect(screen.getByTestId("slicer-selection-summary")).toHaveTextContent("2/3");

    // Click item 1 again to toggle it back on
    fireEvent.click(item1);
    expect(onSelectionChange).toHaveBeenCalledWith(["2", "3", "1"]);
  });

  it("selects all items when clicking 'All' button", () => {
    const onSelectionChange = vi.fn();
    const visual: Visual = {
      name: "PclassSlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["titanic[pclass]"]
    };

    render(<SlicerVisual visual={visual} onSelectionChange={onSelectionChange} />);

    // Clear first
    fireEvent.click(screen.getByTestId("slicer-clear"));
    expect(screen.getByTestId("slicer-selection-summary")).toHaveTextContent("None");

    // Select all
    fireEvent.click(screen.getByTestId("slicer-select-all"));
    expect(screen.getByTestId("slicer-selection-summary")).toHaveTextContent("All");
    expect(onSelectionChange).toHaveBeenCalledWith(["1", "2", "3"]);
  });

  it("clears all selections when clicking 'Clear' button", () => {
    const onSelectionChange = vi.fn();
    const visual: Visual = {
      name: "PclassSlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["titanic[pclass]"]
    };

    render(<SlicerVisual visual={visual} onSelectionChange={onSelectionChange} />);

    fireEvent.click(screen.getByTestId("slicer-clear"));
    expect(screen.getByTestId("slicer-selection-summary")).toHaveTextContent("None");
    expect(onSelectionChange).toHaveBeenCalledWith([]);
  });

  it("filters items dynamically via search input", () => {
    const visual: Visual = {
      name: "RegionSlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["Sales[Region]"]
    };

    render(<SlicerVisual visual={visual} />);

    expect(screen.getByText("North America")).toBeInTheDocument();
    expect(screen.getByText("Europe")).toBeInTheDocument();

    const searchInput = screen.getByTestId("slicer-search-input");
    fireEvent.change(searchInput, { target: { value: "Europe" } });

    expect(screen.getByText("Europe")).toBeInTheDocument();
    expect(screen.queryByText("North America")).not.toBeInTheDocument();
  });

  it("renders error state when bound to a DAX measure instead of categorical dimension", () => {
    const visual: Visual = {
      name: "InvalidSlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["Sales[TotalRevenue]"]
    };

    render(<SlicerVisual visual={visual} />);

    expect(screen.getByTestId("slicer-error-InvalidSlicer")).toBeInTheDocument();
    expect(screen.getByText(/Invalid Slicer Dimension Binding/i)).toBeInTheDocument();
    expect(screen.getByText(/Measure 'TotalRevenue' cannot be bound to a Slicer/i)).toBeInTheDocument();
    expect(screen.queryByTestId("slicer-items-list")).not.toBeInTheDocument();
  });

  it("synchronizes selected items when bound dimension changes", () => {
    const visual: Visual = {
      name: "DynamicSlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["titanic[pclass]"]
    };

    const { rerender } = render(<SlicerVisual visual={visual} />);

    expect(screen.getByTestId("slicer-selection-summary")).toHaveTextContent("All");
    expect(screen.getByTestId("slicer-item-1")).toBeInTheDocument();

    // Rerender with a different dimension (e.g. sex)
    const updatedVisual: Visual = {
      ...visual,
      boundFields: ["titanic[sex]"]
    };
    rerender(<SlicerVisual visual={updatedVisual} />);

    expect(screen.getByTestId("slicer-selection-summary")).toHaveTextContent("All");
    expect(screen.getByTestId("slicer-item-male")).toBeInTheDocument();
    expect(screen.getByTestId("slicer-item-female")).toBeInTheDocument();
  });

  it("supports keyboard toggling with Enter and Space keys", () => {
    const onSelectionChange = vi.fn();
    const visual: Visual = {
      name: "A11ySlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: ["titanic[pclass]"]
    };

    render(<SlicerVisual visual={visual} onSelectionChange={onSelectionChange} />);

    const item1 = screen.getByTestId("slicer-item-1");

    // Toggle off with Space key
    fireEvent.keyDown(item1, { key: " " });
    expect(onSelectionChange).toHaveBeenCalledWith(["2", "3"]);

    // Toggle back on with Enter key
    fireEvent.keyDown(item1, { key: "Enter" });
    expect(onSelectionChange).toHaveBeenCalledWith(["2", "3", "1"]);
  });

  it("handles visual with empty boundFields gracefully", () => {
    const visual: Visual = {
      name: "EmptySlicer",
      visualType: "slicer",
      layout: { x: 20, y: 20, width: 240, height: 260, z: 1, visible: true },
      boundFields: []
    };

    render(<SlicerVisual visual={visual} />);

    expect(screen.getByTestId("slicer-EmptySlicer")).toBeInTheDocument();
    expect(screen.getByText("Dimension Filter")).toBeInTheDocument();
  });
});
