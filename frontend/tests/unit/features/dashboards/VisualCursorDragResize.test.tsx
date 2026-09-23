import { describe, it, expect, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { VisualLayoutEditor } from "@features/dashboards/components/VisualLayoutEditor";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";
import type { Dashboard } from "@entities/visual/types";

describe("VisualLayoutEditor - Cursor Drag & Resize Control", () => {
  const initialDashboard: Dashboard = {
    id: "dash-1",
    name: "Interactive Dashboard",
    pages: [
      {
        name: "Overview",
        canvasWidth: 1280,
        canvasHeight: 720,
        visuals: [
          {
            name: "TestVisual",
            visualType: "lineChart",
            layout: { x: 50, y: 50, width: 400, height: 300, z: 1, visible: true },
            boundFields: ["Orders[OrderDate]", "Orders[TotalSales]"]
          }
        ]
      }
    ]
  };

  beforeEach(() => {
    useDashboardStore.setState({ current: initialDashboard });
  });

  it("renders drag header and corner resize handle", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    const header = screen.getByTestId("visual-header-TestVisual");
    expect(header).toBeInTheDocument();
    expect(header).toHaveAttribute("title", expect.stringMatching(/drag/i));

    const resizeHandle = screen.getByTestId("visual-resize-handle");
    expect(resizeHandle).toBeInTheDocument();
    expect(resizeHandle).toHaveAttribute("title", expect.stringMatching(/resize/i));
  });

  it("updates visual (x, y) coordinates when dragged by cursor on the header", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    const header = screen.getByTestId("visual-header-TestVisual");

    // Mouse down at (100, 100)
    fireEvent.mouseDown(header, { clientX: 100, clientY: 100, button: 0 });

    // Move to (160, 180) -> deltaX = +60, deltaY = +80
    fireEvent.mouseMove(document, { clientX: 160, clientY: 180 });

    // Check that store updated visual layout x = 50 + 60 = 110, y = 50 + 80 = 130
    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updated.layout.x).toBe(110);
    expect(updated.layout.y).toBe(130);

    // Mouse up to finish drag
    fireEvent.mouseUp(document);
  });

  it("normalizes drag movement by canvas scale factor", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    // Scale is 0.5x, so 100px screen movement corresponds to 200px canvas movement
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
        scale={0.5}
      />
    );

    const header = screen.getByTestId("visual-header-TestVisual");
    fireEvent.mouseDown(header, { clientX: 100, clientY: 100, button: 0 });
    fireEvent.mouseMove(document, { clientX: 150, clientY: 140 }); // +50px, +40px screen -> +100px, +80px canvas

    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updated.layout.x).toBe(150); // 50 + 100
    expect(updated.layout.y).toBe(130); // 50 + 80

    fireEvent.mouseUp(document);
  });

  it("updates visual (width, height) when dragging the corner resize handle", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    const resizeHandle = screen.getByTestId("visual-resize-handle");

    // Mouse down at (500, 400)
    fireEvent.mouseDown(resizeHandle, { clientX: 500, clientY: 400, button: 0 });

    // Drag outward +100px width, +50px height
    fireEvent.mouseMove(document, { clientX: 600, clientY: 450 });

    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(updated.layout.width).toBe(500); // 400 + 100
    expect(updated.layout.height).toBe(350); // 300 + 50

    fireEvent.mouseUp(document);
  });

  it("clamps resize dimensions to minimum recommended dimensions", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    const resizeHandle = screen.getByTestId("visual-resize-handle");
    fireEvent.mouseDown(resizeHandle, { clientX: 500, clientY: 400, button: 0 });

    // Drag inward excessively to attempt collapsing below min
    fireEvent.mouseMove(document, { clientX: 100, clientY: 100 });

    const updated = useDashboardStore.getState().current!.pages[0].visuals[0];
    // lineChart minW is 260, minH is 200
    expect(updated.layout.width).toBeGreaterThanOrEqual(260);
    expect(updated.layout.height).toBeGreaterThanOrEqual(200);

    fireEvent.mouseUp(document);
  });

  it("does not initiate drag when clicking interactive dropdown controls in the header", () => {
    const visual = useDashboardStore.getState().current!.pages[0].visuals[0];
    render(
      <VisualLayoutEditor
        pageName="Overview"
        visual={visual}
        canvasWidth={1280}
        canvasHeight={720}
      />
    );

    const typeSelect = screen.getByLabelText("select-type-TestVisual");
    fireEvent.pointerDown(typeSelect, { clientX: 100, clientY: 100, button: 0 });
    fireEvent.pointerMove(window, { clientX: 200, clientY: 200 });

    // Visual position should NOT have changed
    const current = useDashboardStore.getState().current!.pages[0].visuals[0];
    expect(current.layout.x).toBe(50);
    expect(current.layout.y).toBe(50);
  });
});
