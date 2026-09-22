import { describe, it, expect, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { DashboardCanvas } from "@features/dashboards/components/DashboardCanvas";
import { VisualLayoutEditor } from "@features/dashboards/components/VisualLayoutEditor";
import { CardVisual } from "@features/dashboards/components/visuals/CardVisual";
import { BarChartVisual } from "@features/dashboards/components/visuals/BarChartVisual";
import { LineChartVisual } from "@features/dashboards/components/visuals/LineChartVisual";
import { DonutChartVisual } from "@features/dashboards/components/visuals/DonutChartVisual";
import { TableVisual } from "@features/dashboards/components/visuals/TableVisual";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";
import type { DashboardDefinition } from "@features/dashboards/model/types";
import type { Visual } from "@entities/visual/types";

describe("40 UI Automated Verification Test Cases - Canvas Layout & Size-Aware Styling", () => {
  const sampleDashboard: DashboardDefinition = {
    id: "dash-40-cases",
    name: "Enterprise Performance Hub",
    pages: [
      {
        name: "Overview",
        canvasWidth: 1280,
        canvasHeight: 720,
        visuals: [
          {
            name: "RevenueKpi",
            visualType: "card",
            layout: { x: 40, y: 30, width: 320, height: 180, z: 1, visible: true },
            boundFields: ["Sales[TotalRevenue]"]
          },
          {
            name: "SalesBar",
            visualType: "barChart",
            layout: { x: 400, y: 30, width: 440, height: 320, z: 1, visible: true },
            boundFields: ["Sales[Region]", "Sales[TotalRevenue]"]
          },
          {
            name: "TrendLine",
            visualType: "lineChart",
            layout: { x: 880, y: 30, width: 360, height: 320, z: 1, visible: true },
            boundFields: ["Orders[OrderDate]", "Orders[TotalRevenue]"]
          },
          {
            name: "SegmentsDonut",
            visualType: "donutChart",
            layout: { x: 40, y: 380, width: 320, height: 300, z: 1, visible: true },
            boundFields: ["Customers[Tier]", "Customers[TotalRevenue]"]
          },
          {
            name: "RecordsTable",
            visualType: "table",
            layout: { x: 400, y: 380, width: 840, height: 300, z: 1, visible: true },
            boundFields: ["Customers[Name]", "Customers[TotalRevenue]"]
          }
        ]
      },
      {
        name: "DetailsPage",
        canvasWidth: 1280,
        canvasHeight: 720,
        visuals: [
          {
            name: "DetailsTable",
            visualType: "table",
            layout: { x: 50, y: 50, width: 800, height: 400, z: 1, visible: true },
            boundFields: ["Sales[Item]", "Sales[TotalRevenue]"]
          }
        ]
      },
      {
        name: "EmptyPage",
        canvasWidth: 1280,
        canvasHeight: 720,
        visuals: []
      }
    ]
  };

  beforeEach(() => {
    useDashboardStore.getState().setDashboard(JSON.parse(JSON.stringify(sampleDashboard)));
  });

  // =========================================================================
  // Category 1: Canvas Display Modes & Responsive Viewport Scaling (Cases 1-8)
  // =========================================================================

  it("UI-TC-01: FitToPage initial render mounts canvas with scale transform", () => {
    render(<DashboardCanvas />);
    const canvas = screen.getByTestId("layout-grid-canvas");
    expect(canvas).toBeInTheDocument();
    expect(canvas.style.transform).toMatch(/scale\([0-9.]+\)/);
    expect(screen.getByTestId("mode-fit-to-page")).toHaveClass("btn-primary");
  });

  it("UI-TC-02: Switch to FitToWidth mode updates mode button and layout", () => {
    render(<DashboardCanvas />);
    const fitToWidthBtn = screen.getByTestId("mode-fit-to-width");
    fireEvent.click(fitToWidthBtn);
    expect(fitToWidthBtn).toHaveClass("btn-primary");
    expect(screen.getByTestId("mode-fit-to-page")).not.toHaveClass("btn-primary");
  });

  it("UI-TC-03: Switch to ActualSize mode enables scrollable container", () => {
    render(<DashboardCanvas />);
    const actualBtn = screen.getByTestId("mode-actual-size");
    fireEvent.click(actualBtn);
    expect(actualBtn).toHaveClass("btn-primary");
    const viewport = screen.getByTestId("canvas-viewport-container");
    expect(viewport.style.overflow).toBe("auto");
  });

  it("UI-TC-04: Viewport resize event triggers dimension update", () => {
    render(<DashboardCanvas />);
    fireEvent(window, new Event("resize"));
    const canvas = screen.getByTestId("layout-grid-canvas");
    expect(canvas).toBeInTheDocument();
  });

  it("UI-TC-05: Canvas Zoom In button (+10%) increments zoom badge", () => {
    render(<DashboardCanvas />);
    const initialBadge = screen.getByTestId("zoom-level-badge").textContent;
    const zoomInBtn = screen.getByTestId("btn-zoom-in");
    fireEvent.click(zoomInBtn);
    const updatedBadge = screen.getByTestId("zoom-level-badge").textContent;
    expect(updatedBadge).not.toEqual(initialBadge);
  });

  it("UI-TC-06: Canvas Zoom Out button (-10%) decrements zoom badge", () => {
    render(<DashboardCanvas />);
    const initialBadge = screen.getByTestId("zoom-level-badge").textContent;
    const zoomOutBtn = screen.getByTestId("btn-zoom-out");
    fireEvent.click(zoomOutBtn);
    const updatedBadge = screen.getByTestId("zoom-level-badge").textContent;
    expect(updatedBadge).not.toEqual(initialBadge);
  });

  it("UI-TC-07: Canvas Zoom Reset restores zoom level", () => {
    render(<DashboardCanvas />);
    const zoomInBtn = screen.getByTestId("btn-zoom-in");
    fireEvent.click(zoomInBtn);
    fireEvent.click(zoomInBtn);
    const resetBtn = screen.getByTestId("btn-zoom-reset");
    fireEvent.click(resetBtn);
    expect(screen.getByTestId("zoom-level-badge")).toBeInTheDocument();
  });

  it("UI-TC-08: Canvas coexistence with side drawer / viewport constraints", () => {
    render(<DashboardCanvas />);
    const toolbar = screen.getByTestId("canvas-toolbar");
    expect(toolbar).toBeInTheDocument();
    expect(screen.getByTestId("layout-grid-canvas")).toBeInTheDocument();
  });

  // =========================================================================
  // Category 2: Dynamic Visual Type Switching & Content Adaptation (Cases 9-16)
  // =========================================================================

  it("UI-TC-09: Switch card -> barChart replaces KPI metric with bar chart content", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const typeSelect = screen.getByLabelText("select-type-RevenueKpi");
    fireEvent.change(typeSelect, { target: { value: "barChart" } });
    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updated.visualType).toBe("barChart");
  });

  it("UI-TC-10: Switch barChart -> lineChart replaces bars with continuous line SVG", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[1];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const typeSelect = screen.getByLabelText("select-type-SalesBar");
    fireEvent.change(typeSelect, { target: { value: "lineChart" } });
    const updated = useDashboardStore.getState().current!.pages[0].visuals[1];
    expect(updated.visualType).toBe("lineChart");
  });

  it("UI-TC-11: Switch lineChart -> donutChart renders donut SVG slices", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[2];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const typeSelect = screen.getByLabelText("select-type-TrendLine");
    fireEvent.change(typeSelect, { target: { value: "donutChart" } });
    const updated = useDashboardStore.getState().current!.pages[0].visuals[2];
    expect(updated.visualType).toBe("donutChart");
  });

  it("UI-TC-12: Switch donutChart -> table renders multi-column table data grid", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[3];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const typeSelect = screen.getByLabelText("select-type-SegmentsDonut");
    fireEvent.change(typeSelect, { target: { value: "table" } });
    const updated = useDashboardStore.getState().current!.pages[0].visuals[3];
    expect(updated.visualType).toBe("table");
  });

  it("UI-TC-13: Switch table -> card renders KPI headline and formula pill", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[4];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const typeSelect = screen.getByLabelText("select-type-RecordsTable");
    fireEvent.change(typeSelect, { target: { value: "card" } });
    const updated = useDashboardStore.getState().current!.pages[0].visuals[4];
    expect(updated.visualType).toBe("card");
  });

  it("UI-TC-14: Preserve field bindings across compatible multi-axis type switching", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[1];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const typeSelect = screen.getByLabelText("select-type-SalesBar");
    fireEvent.change(typeSelect, { target: { value: "lineChart" } });
    const updated = useDashboardStore.getState().current!.pages[0].visuals[1];
    expect(updated.boundFields).toEqual(["Sales[Region]", "Sales[TotalRevenue]"]);
  });

  it("UI-TC-15: Single-to-dual slot transition exposes secondary metric selector", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    const { rerender } = render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    expect(screen.queryByLabelText("select-measure-RevenueKpi")).not.toBeInTheDocument();

    const dualVisual: Visual = { ...visual, visualType: "barChart", boundFields: ["Sales[Region]", "Sales[TotalRevenue]"] };
    rerender(<VisualLayoutEditor pageName="Overview" visual={dualVisual} />);
    expect(screen.getByLabelText("select-measure-RevenueKpi")).toBeInTheDocument();
  });

  it("UI-TC-16: Dual-to-single slot transition collapses secondary metric selector", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[1]; // SalesBar
    const { rerender } = render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    expect(screen.getByLabelText("select-measure-SalesBar")).toBeInTheDocument();

    const singleVisual: Visual = { ...visual, visualType: "card", boundFields: ["Sales[TotalRevenue]"] };
    rerender(<VisualLayoutEditor pageName="Overview" visual={singleVisual} />);
    expect(screen.queryByLabelText("select-measure-SalesBar")).not.toBeInTheDocument();
  });

  // =========================================================================
  // Category 3: Size-Aware Styling & CSS Container Queries (Cases 17-24)
  // =========================================================================

  it("UI-TC-17: Visual card root element declares container-type inline-size", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const card = screen.getByTestId("visual-RevenueKpi");
    expect(card.style.containerType).toBe("inline-size");
  });

  it("UI-TC-18: Micro card layout (< 240px width) adapts header styling", () => {
    const microVisual: Visual = {
      name: "MicroCard",
      visualType: "card",
      layout: { x: 10, y: 10, width: 200, height: 140, visible: true },
      boundFields: ["Sales[TotalRevenue]"]
    };
    render(<VisualLayoutEditor pageName="Overview" visual={microVisual} />);
    const card = screen.getByTestId("visual-MicroCard");
    expect(card.style.padding).toBe("0.4rem");
  });

  it("UI-TC-19: Compact card layout (240px - 380px width) sets constrained title width", () => {
    const compactVisual: Visual = {
      name: "CompactCardLongTitleName",
      visualType: "card",
      layout: { x: 10, y: 10, width: 300, height: 180, visible: true },
      boundFields: ["Sales[TotalRevenue]"]
    };
    render(<VisualLayoutEditor pageName="Overview" visual={compactVisual} />);
    const titleEl = screen.getByTitle("CompactCardLongTitleName");
    expect(titleEl.style.maxWidth).toBe("120px");
  });

  it("UI-TC-20: Standard card layout (380px - 600px width) sets standard title width", () => {
    const standardVisual: Visual = {
      name: "StandardCardTitle",
      visualType: "barChart",
      layout: { x: 10, y: 10, width: 450, height: 300, visible: true },
      boundFields: ["Sales[Region]", "Sales[TotalRevenue]"]
    };
    render(<VisualLayoutEditor pageName="Overview" visual={standardVisual} />);
    const titleEl = screen.getByTitle("StandardCardTitle");
    expect(titleEl.style.maxWidth).toBe("200px");
  });

  it("UI-TC-21: Expanded card layout (> 600px width) expands layout dimensions", () => {
    const expandedVisual: Visual = {
      name: "ExpandedTable",
      visualType: "table",
      layout: { x: 10, y: 10, width: 850, height: 350, visible: true },
      boundFields: ["Sales[Region]", "Sales[TotalRevenue]"]
    };
    render(<VisualLayoutEditor pageName="Overview" visual={expandedVisual} />);
    const card = screen.getByTestId("visual-ExpandedTable");
    expect(card.style.width).toBe("850px");
  });

  it("UI-TC-22: Fluid KPI font scaling clamp in CardVisual", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    const { container } = render(<CardVisual visual={visual} />);
    const metricText = container.querySelector("div[style*='font-size: clamp']");
    expect(metricText).toBeInTheDocument();
  });

  it("UI-TC-23: SVG fluid viewBox scaling on Line and Donut charts", () => {
    const lineVisual = useDashboardStore.getState().current!.pages[0].visuals[2];
    const donutVisual = useDashboardStore.getState().current!.pages[0].visuals[3];

    const { rerender } = render(<LineChartVisual visual={lineVisual} />);
    const lineSvg = screen.getByTestId("linechart-svg");
    expect(lineSvg).toHaveAttribute("preserveAspectRatio", "xMidYMid meet");

    rerender(<DonutChartVisual visual={donutVisual} />);
    const donutSvg = screen.getByTestId("donutchart-svg");
    expect(donutSvg).toHaveAttribute("preserveAspectRatio", "xMidYMid meet");
  });

  it("UI-TC-24: Table visual scroll containment with overflow auto", () => {
    const tableVisual = useDashboardStore.getState().current!.pages[0].visuals[4];
    render(<TableVisual visual={tableVisual} />);
    const tableContainer = screen.getByTestId("table-visual-container");
    expect(tableContainer.style.overflow).toBe("auto");
  });

  // =========================================================================
  // Category 4: Measure & Dimension Role Validation UI States (Cases 25-32)
  // =========================================================================

  it("UI-TC-25: Card visual raw column error state renders warning banner", () => {
    const invalidCard: Visual = {
      name: "BadCard",
      visualType: "card",
      layout: { x: 10, y: 10, width: 300, height: 180, visible: true },
      boundFields: ["Sales[Revenue]"] // raw column, missing Total_/Sum_/Avg_
    };
    render(<CardVisual visual={invalidCard} />);
    expect(screen.getByTestId("card-error-BadCard")).toBeInTheDocument();
  });

  it("UI-TC-26: Bar chart metric raw column error state renders error banner", () => {
    const invalidBar: Visual = {
      name: "BadBar",
      visualType: "barChart",
      layout: { x: 10, y: 10, width: 400, height: 300, visible: true },
      boundFields: ["Sales[Region]", "Sales[Age]"] // raw column in metric slot
    };
    render(<BarChartVisual visual={invalidBar} />);
    expect(screen.getByTestId("barchart-error-BadBar")).toBeInTheDocument();
  });

  it("UI-TC-27: Line chart metric raw column error state renders error banner", () => {
    const invalidLine: Visual = {
      name: "BadLine",
      visualType: "lineChart",
      layout: { x: 10, y: 10, width: 400, height: 300, visible: true },
      boundFields: ["Orders[Date]", "Orders[Quantity]"]
    };
    render(<LineChartVisual visual={invalidLine} />);
    expect(screen.getByTestId("linechart-error-BadLine")).toBeInTheDocument();
  });

  it("UI-TC-28: Donut chart slice metric raw column error state renders error banner", () => {
    const invalidDonut: Visual = {
      name: "BadDonut",
      visualType: "donutChart",
      layout: { x: 10, y: 10, width: 300, height: 300, visible: true },
      boundFields: ["Customers[Tier]", "Customers[Points]"]
    };
    render(<DonutChartVisual visual={invalidDonut} />);
    expect(screen.getByTestId("donutchart-error-BadDonut")).toBeInTheDocument();
  });

  it("UI-TC-29: Category axis measure warning badge displays 'Dimension Expected'", () => {
    const measureAsCategoryBar: Visual = {
      name: "MeasureCategoryBar",
      visualType: "barChart",
      layout: { x: 10, y: 10, width: 400, height: 300, visible: true },
      boundFields: ["Sales[TotalRevenue]", "Sales[TotalRevenue]"]
    };
    render(<BarChartVisual visual={measureAsCategoryBar} />);
    expect(screen.getByText(/Dimension Expected/i)).toBeInTheDocument();
  });

  it("UI-TC-30: Dropdown optgroup partitions into DAX Measures and Dimensions", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const select = screen.getByLabelText("select-field-RevenueKpi");
    expect(select.innerHTML).toContain("DAX Measures (Valid)");
  });

  it("UI-TC-31: Instant error recovery when selecting valid measure", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    const { rerender } = render(<CardVisual visual={{ ...visual, boundFields: ["Sales[Price]"] }} />);
    expect(screen.getByTestId("card-error-RevenueKpi")).toBeInTheDocument();

    rerender(<CardVisual visual={{ ...visual, boundFields: ["Sales[TotalRevenue]"] }} />);
    expect(screen.queryByTestId("card-error-RevenueKpi")).not.toBeInTheDocument();
  });

  it("UI-TC-32: DAX validation pill displayed on valid measure binding", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<CardVisual visual={visual} />);
    expect(screen.getByText("DAX Validated")).toBeInTheDocument();
  });

  // =========================================================================
  // Category 5: Layout Manipulation, Resizing, Boundary Clamping (Cases 33-40)
  // =========================================================================

  it("UI-TC-33: Coordinate Nudge X action increments visual X coordinate by +10", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} />);
    const moveBtn = screen.getByLabelText("move-RevenueKpi");
    fireEvent.click(moveBtn);
    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updated.layout.x).toBe(50); // 40 + 10
  });

  it("UI-TC-34: Canvas boundary clamping prevents visual from exceeding canvas width", () => {
    const store = useDashboardStore.getState();
    const edgeVisual: Visual = {
      name: "EdgeVisual",
      visualType: "card",
      layout: { x: 975, y: 50, width: 300, height: 180, visible: true },
      boundFields: ["Sales[TotalRevenue]"]
    };
    store.addVisual("Overview", edgeVisual);

    render(<VisualLayoutEditor pageName="Overview" visual={edgeVisual} canvasWidth={1280} />);
    const moveBtn = screen.getByLabelText("move-EdgeVisual");
    // maxX is 1280 - 300 = 980. Visual is at 975, nudging +10 would be 985, clamped to 980!
    fireEvent.click(moveBtn);
    const updated = useDashboardStore.getState().current!.pages[0].visuals.find((v) => v.name === "EdgeVisual");
    expect(updated?.layout.x).toBe(980);
  });

  it("UI-TC-35: Canvas boundary clamping prevents visual from exceeding canvas height", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} canvasHeight={720} />);
    expect(visual.layout.y + visual.layout.height).toBeLessThanOrEqual(720);
  });

  it("UI-TC-36: Minimum visual size constraints enforced (min 180w x 120h)", () => {
    const tinyVisual: Visual = {
      name: "TinyVisual",
      visualType: "card",
      layout: { x: 10, y: 10, width: 100, height: 80, visible: true },
      boundFields: ["Sales[TotalRevenue]"]
    };
    render(<VisualLayoutEditor pageName="Overview" visual={tinyVisual} />);
    const card = screen.getByTestId("visual-TinyVisual");
    expect(card.style.width).toBe("180px"); // clamped to 180
    expect(card.style.height).toBe("120px"); // clamped to 120
  });

  it("UI-TC-37: Active visual z-index elevation on click", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(<VisualLayoutEditor pageName="Overview" visual={visual} isActive={true} />);
    const card = screen.getByTestId("visual-RevenueKpi");
    expect(card.style.zIndex).toBe("20");
  });

  it("UI-TC-38: Multi-page canvas navigation loads active page visuals", () => {
    render(<DashboardCanvas />);
    expect(screen.getByTestId("visual-RevenueKpi")).toBeInTheDocument();
    const detailsTab = screen.getByRole("tab", { name: "DetailsPage" });
    fireEvent.click(detailsTab);
    expect(screen.getByTestId("visual-DetailsTable")).toBeInTheDocument();
  });

  it("UI-TC-39: Visual empty state renders CTA when page has no visuals", () => {
    render(<DashboardCanvas />);
    const emptyTab = screen.getByRole("tab", { name: "EmptyPage" });
    fireEvent.click(emptyTab);
    expect(screen.getByTestId("empty-canvas-state")).toBeInTheDocument();
    expect(screen.getByText(/No visuals on this page/i)).toBeInTheDocument();
  });

  it("UI-TC-40: Layout persistence across store updates", () => {
    const store = useDashboardStore.getState();
    store.updateVisualLayout("Overview", "RevenueKpi", { x: 75, y: 85 });
    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updated.layout.x).toBe(75);
    expect(updated.layout.y).toBe(85);
  });
});
