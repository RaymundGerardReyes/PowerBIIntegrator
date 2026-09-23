import React from "react";
import { describe, it, expect } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { DonutChartVisual } from "@features/dashboards/components/visuals/DonutChartVisual";
import { VisualLayoutEditor, RECOMMENDED_VISUAL_DIMENSIONS } from "@features/dashboards/components/VisualLayoutEditor";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";
import type { Visual } from "@entities/visual/types";

describe("DonutChartVisual Fluid Scaling & Type Switching Dimension Adaptation", () => {
  it("renders DonutChart in compact height container (160px) without overflow clipping", () => {
    const compactVisual: Visual = {
      name: "CompactDonut",
      visualType: "donutChart",
      layout: { x: 420, y: 30, width: 360, height: 160, visible: true },
      boundFields: ["titanic[pclass]", "TotalRows"]
    };

    render(<DonutChartVisual visual={compactVisual} />);

    // SVG must be rendered with data-testid
    const svg = screen.getByTestId("donutchart-svg");
    expect(svg).toBeInTheDocument();
    expect(svg).toHaveAttribute("viewBox", "0 0 110 110");
    expect(svg).toHaveAttribute("preserveAspectRatio", "xMidYMid meet");

    // In compact mode (< 220px), subtitle is omitted to maximize SVG visibility
    expect(screen.queryByText(/Ring Ratio Distribution/i)).not.toBeInTheDocument();

    // Legend should be rendered
    expect(screen.getByText("3rd Class")).toBeInTheDocument();
    expect(screen.getByText("54%")).toBeInTheDocument();
  });

  it("renders DonutChart in standard container (300px) with full subtitle and legend", () => {
    const standardVisual: Visual = {
      name: "StandardDonut",
      visualType: "donutChart",
      layout: { x: 20, y: 20, width: 400, height: 300, visible: true },
      boundFields: ["titanic[sex]", "TotalRows"]
    };

    render(<DonutChartVisual visual={standardVisual} />);

    expect(screen.getByText(/Ring Ratio Distribution/i)).toBeInTheDocument();
    expect(screen.getByText("Male")).toBeInTheDocument();
    expect(screen.getByText("64%")).toBeInTheDocument();
  });

  it("renders PieChart visual type correctly with full proportional slices title", () => {
    const pieVisual: Visual = {
      name: "AgePie",
      visualType: "pieChart",
      layout: { x: 20, y: 20, width: 400, height: 300, visible: true },
      boundFields: ["titanic[Average_age]", "TotalRows"]
    };

    render(<DonutChartVisual visual={pieVisual} />);

    expect(screen.getByText(/Full Proportional Slices/i)).toBeInTheDocument();
    expect(screen.getByText(/Pie Chart by Average_age/i)).toBeInTheDocument();
  });

  it("automatically adapts card height when switching from 160px KPI Card to Donut Chart", () => {
    const initialVisual: Visual = {
      name: "TestVisual",
      visualType: "card",
      layout: { x: 40, y: 30, width: 300, height: 160, visible: true },
      boundFields: ["titanic[pclass]", "TotalRows"]
    };

    useDashboardStore.setState({
      current: {
        id: "dash-1",
        name: "Test Dash",
        pages: [
          {
            name: "Overview",
            canvasWidth: 1280,
            canvasHeight: 720,
            visuals: [initialVisual]
          }
        ]
      }
    });

    render(<VisualLayoutEditor pageName="Overview" visual={initialVisual} />);

    const typeSelect = screen.getByLabelText("select-type-TestVisual");
    fireEvent.change(typeSelect, { target: { value: "donutChart" } });

    const updatedVisual = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updatedVisual.visualType).toBe("donutChart");

    // Height should be upgraded from 160 to at least recommended height (260)
    expect(updatedVisual.layout.height).toBeGreaterThanOrEqual(RECOMMENDED_VISUAL_DIMENSIONS.donutChart.minH);
    expect(updatedVisual.layout.height).toBe(260);
  });
});
