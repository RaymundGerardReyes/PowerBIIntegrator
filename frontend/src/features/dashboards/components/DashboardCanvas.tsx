import React, { useState, useEffect, useRef, useLayoutEffect, useCallback, useMemo } from "react";
import { LayoutGrid, CanvasDisplayOption } from "@shared/ui/LayoutGrid/LayoutGrid";
import { PageSelector } from "./PageSelector";
import { VisualLayoutEditor } from "./VisualLayoutEditor";
import { useDashboardStore } from "../model/dashboardSlice";

export const DashboardCanvas: React.FC = () => {
  const dashboard = useDashboardStore((s) => s.current);
  const addVisual = useDashboardStore((s) => s.addVisual);
  const addPage = useDashboardStore((s) => s.addPage);
  const removePage = useDashboardStore((s) => s.removePage);
  const moveVisualToPage = useDashboardStore((s) => s.moveVisualToPage);
  const rearrangePageVisuals = useDashboardStore((s) => s.rearrangePageVisuals);

  const [selectedPage, setSelectedPage] = useState<string>(dashboard?.pages[0]?.name ?? "");
  const [displayOption, setDisplayOption] = useState<CanvasDisplayOption>("FitToPage");
  const [manualZoom, setManualZoom] = useState<number>(1.0);
  const [containerDimensions, setContainerDimensions] = useState<{ width: number; height: number }>({
    width: 1200,
    height: 700
  });
  const [activeVisual, setActiveVisual] = useState<string | null>(null);

  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (dashboard?.pages && dashboard.pages.length > 0) {
      if (!dashboard.pages.some((p) => p.name === selectedPage)) {
        setSelectedPage(dashboard.pages[0].name);
      }
    }
  }, [dashboard, selectedPage]);

  // Track container width and height with ResizeObserver
  const updateDimensions = useCallback(() => {
    if (containerRef.current) {
      const { clientWidth, clientHeight } = containerRef.current;
      setContainerDimensions({
        width: clientWidth > 0 ? clientWidth - 32 : 1200, // accounting for container padding
        height: clientHeight > 0 ? clientHeight - 32 : 700
      });
    }
  }, []);

  useLayoutEffect(() => {
    updateDimensions();

    const node = containerRef.current;
    if (!node) return;

    if (typeof ResizeObserver !== "undefined") {
      const observer = new ResizeObserver(() => {
        updateDimensions();
      });
      observer.observe(node);
      return () => observer.disconnect();
    } else {
      window.addEventListener("resize", updateDimensions);
      return () => window.removeEventListener("resize", updateDimensions);
    }
  }, [updateDimensions]);

  const page = dashboard?.pages.find((p) => p.name === selectedPage) ?? dashboard?.pages[0];

  // Congestion & Canvas Capacity Detection
  const isCrowded = useMemo(() => {
    if (!page || page.visuals.length < 3) return false;
    // Condition 1: 5 or more visuals on single canvas
    if (page.visuals.length >= 5) return true;
    // Condition 2: Visual bounding box collisions
    for (let i = 0; i < page.visuals.length; i++) {
      for (let j = i + 1; j < page.visuals.length; j++) {
        const a = page.visuals[i].layout;
        const b = page.visuals[j].layout;
        const overlapX = a.x < b.x + b.width && a.x + a.width > b.x;
        const overlapY = a.y < b.y + b.height && a.y + a.height > b.y;
        if (overlapX && overlapY) return true;
      }
    }
    // Condition 3: Total visual area exceeds 75% of canvas area
    const totalArea = page.visuals.reduce((acc, v) => acc + v.layout.width * v.layout.height, 0);
    const canvasArea = (page.canvasWidth || 1280) * (page.canvasHeight || 720);
    return totalArea > 0.75 * canvasArea;
  }, [page]);

  if (!dashboard) return <p>No dashboard loaded.</p>;
  if (!page) return <p>No pages available.</p>;

  // Compute responsive scale factor based on active mode
  let baseScale = 1.0;
  const canvasWidth = page.canvasWidth || 1280;
  const canvasHeight = page.canvasHeight || 720;

  if (displayOption === "FitToPage") {
    const scaleX = containerDimensions.width / canvasWidth;
    const scaleY = containerDimensions.height > 200 ? containerDimensions.height / canvasHeight : scaleX;
    baseScale = Math.min(scaleX, scaleY);
  } else if (displayOption === "FitToWidth") {
    baseScale = containerDimensions.width / canvasWidth;
  } else {
    baseScale = 1.0;
  }

  // Combined scale factor clamped within safe boundaries (0.25x to 2.5x)
  const effectiveScale = Math.max(0.25, Math.min(2.5, Math.round(baseScale * manualZoom * 100) / 100));

  const handleZoomIn = () => setManualZoom((z) => Math.min(2.0, Math.round((z + 0.1) * 10) / 10));
  const handleZoomOut = () => setManualZoom((z) => Math.max(0.25, Math.round((z - 0.1) * 10) / 10));
  const handleZoomReset = () => setManualZoom(1.0);

  const handleAddNewPage = () => {
    const newName = addPage();
    if (newName) {
      setSelectedPage(newName);
    }
  };

  const handleDeletePage = (pageNameToDelete: string) => {
    if (dashboard.pages.length <= 1) return;
    removePage(pageNameToDelete);
    if (selectedPage === pageNameToDelete) {
      const remaining = dashboard.pages.filter((p) => p.name !== pageNameToDelete);
      if (remaining.length > 0) {
        setSelectedPage(remaining[0].name);
      }
    }
  };

  const handleDistributeToNewPage = () => {
    if (!page || page.visuals.length <= 2) return;
    const newPageName = addPage();
    const countToMove = Math.min(2, Math.floor(page.visuals.length / 2));
    const visualsToMove = page.visuals.slice(-countToMove);
    visualsToMove.forEach((v) => {
      moveVisualToPage(page.name, newPageName, v.name);
    });
    rearrangePageVisuals(page.name);
    setSelectedPage(newPageName);
  };

  const handleAddSampleVisual = () => {
    addVisual(page.name, {
      name: `Visual_${Date.now() % 10000}`,
      visualType: "card",
      layout: {
        x: 40,
        y: 40,
        width: 320,
        height: 180,
        visible: true
      },
      boundFields: ["TotalRows"]
    });
  };

  return (
    <div
      ref={containerRef}
      style={{
        display: "flex",
        flexDirection: "column",
        width: "100%",
        gap: "0.75rem",
        boxSizing: "border-box"
      }}
    >
      {/* Top Header Controls: Page Selector & Canvas Toolbar */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          flexWrap: "wrap",
          gap: "0.5rem",
          padding: "0.25rem 0"
        }}
      >
        <PageSelector
          pages={dashboard.pages}
          selected={page.name}
          onSelect={setSelectedPage}
          onAddPage={handleAddNewPage}
          onDeletePage={handleDeletePage}
        />

        {/* Canvas Display Modes, Zoom & Smart Auto-Rearrange Toolbar */}
        <div
          data-testid="canvas-toolbar"
          style={{
            display: "flex",
            alignItems: "center",
            gap: "0.5rem",
            backgroundColor: "var(--bg-card, #ffffff)",
            padding: "4px 8px",
            borderRadius: "6px",
            border: "1px solid var(--border-color, #e2e8f0)",
            boxShadow: "var(--shadow-sm, 0 1px 2px 0 rgba(0, 0, 0, 0.05))"
          }}
        >
          {/* Smart Auto-Rearrange Action */}
          <button
            data-testid="btn-auto-rearrange"
            onClick={() => rearrangePageVisuals(page.name)}
            className="btn btn-secondary btn-sm"
            style={{
              fontSize: "0.75rem",
              padding: "3px 8px",
              display: "flex",
              alignItems: "center",
              gap: "4px",
              fontWeight: 500
            }}
            title="Auto-rearrange visuals into an optimal Power BI grid layout"
          >
            <span>✨</span> Auto-Rearrange
          </button>

          <div style={{ width: "1px", height: "18px", backgroundColor: "var(--border-color, #e2e8f0)" }} />

          {/* Display Mode Switcher */}
          <div style={{ display: "flex", alignItems: "center", gap: "2px" }}>
            <button
              data-testid="mode-fit-to-page"
              onClick={() => setDisplayOption("FitToPage")}
              className={`btn btn-sm ${displayOption === "FitToPage" ? "btn-primary" : "btn-ghost"}`}
              style={{
                fontSize: "0.75rem",
                padding: "3px 8px",
                borderRadius: "4px",
                fontWeight: displayOption === "FitToPage" ? 600 : 400
              }}
              title="Fit entire canvas inside viewport without scrolling"
            >
              Fit to Page
            </button>
            <button
              data-testid="mode-fit-to-width"
              onClick={() => setDisplayOption("FitToWidth")}
              className={`btn btn-sm ${displayOption === "FitToWidth" ? "btn-primary" : "btn-ghost"}`}
              style={{
                fontSize: "0.75rem",
                padding: "3px 8px",
                borderRadius: "4px",
                fontWeight: displayOption === "FitToWidth" ? 600 : 400
              }}
              title="Fit canvas to 100% viewport width"
            >
              Fit to Width
            </button>
            <button
              data-testid="mode-actual-size"
              onClick={() => setDisplayOption("ActualSize")}
              className={`btn btn-sm ${displayOption === "ActualSize" ? "btn-primary" : "btn-ghost"}`}
              style={{
                fontSize: "0.75rem",
                padding: "3px 8px",
                borderRadius: "4px",
                fontWeight: displayOption === "ActualSize" ? 600 : 400
              }}
              title="Render actual 1:1 pixel resolution"
            >
              Actual Size
            </button>
          </div>

          <div style={{ width: "1px", height: "18px", backgroundColor: "var(--border-color, #e2e8f0)" }} />

          {/* Interactive Zoom Controls */}
          <div style={{ display: "flex", alignItems: "center", gap: "4px" }}>
            <button
              data-testid="btn-zoom-out"
              onClick={handleZoomOut}
              className="btn btn-ghost btn-sm"
              style={{ padding: "2px 6px", fontSize: "0.8rem", lineHeight: 1 }}
              title="Zoom out (-10%)"
              aria-label="Zoom out"
            >
              −
            </button>
            <span
              data-testid="zoom-level-badge"
              style={{
                fontSize: "0.75rem",
                fontFamily: "monospace",
                fontWeight: 600,
                color: "var(--text-secondary, #475569)",
                minWidth: "42px",
                textAlign: "center"
              }}
            >
              {Math.round(effectiveScale * 100)}%
            </span>
            <button
              data-testid="btn-zoom-in"
              onClick={handleZoomIn}
              className="btn btn-ghost btn-sm"
              style={{ padding: "2px 6px", fontSize: "0.8rem", lineHeight: 1 }}
              title="Zoom in (+10%)"
              aria-label="Zoom in"
            >
              +
            </button>
            <button
              data-testid="btn-zoom-reset"
              onClick={handleZoomReset}
              className="btn btn-ghost btn-sm"
              style={{ padding: "2px 6px", fontSize: "0.7rem", color: "var(--text-muted, #64748b)" }}
              title="Reset zoom to default"
            >
              Reset
            </button>
          </div>
        </div>
      </div>

      {/* Canvas Crowding & Multi-Page Suggestion Banner */}
      {isCrowded && (
        <div
          data-testid="canvas-congestion-banner"
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            padding: "0.5rem 0.85rem",
            backgroundColor: "#fef3c7",
            border: "1px solid #f59e0b",
            borderRadius: "6px",
            fontSize: "0.8125rem",
            color: "#92400e",
            gap: "0.75rem"
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
            <span style={{ fontSize: "1rem" }}>💡</span>
            <span>
              <strong>Canvas layout crowded ({page.visuals.length} visuals):</strong> Visuals may clip or overlap. Auto-rearrange layout or distribute excess visuals to a new page.
            </span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: "0.4rem" }}>
            <button
              data-testid="btn-banner-rearrange"
              onClick={() => rearrangePageVisuals(page.name)}
              className="btn btn-sm"
              style={{
                backgroundColor: "#d97706",
                color: "#ffffff",
                border: "none",
                fontSize: "0.75rem",
                padding: "3px 10px",
                fontWeight: 600,
                borderRadius: "4px"
              }}
            >
              ✨ Auto-Rearrange
            </button>
            <button
              data-testid="btn-banner-add-page"
              onClick={handleDistributeToNewPage}
              className="btn btn-sm"
              style={{
                backgroundColor: "#ffffff",
                border: "1px solid #d97706",
                color: "#92400e",
                fontSize: "0.75rem",
                padding: "3px 10px",
                fontWeight: 600,
                borderRadius: "4px"
              }}
            >
              📄 Add New Page & Distribute
            </button>
          </div>
        </div>
      )}

      {/* Main Canvas Viewport */}
      {page.visuals.length === 0 ? (
        <div
          data-testid="empty-canvas-state"
          style={{
            padding: "3rem 1.5rem",
            textAlign: "center",
            backgroundColor: "var(--bg-canvas-outer, #f8fafc)",
            border: "2px dashed var(--border-color, #cbd5e1)",
            borderRadius: "8px",
            color: "var(--text-secondary, #64748b)"
          }}
        >
          <div style={{ fontSize: "2rem", marginBottom: "0.5rem" }}>📊</div>
          <h3 style={{ margin: "0 0 0.5rem 0", color: "var(--text-primary, #1e293b)" }}>No visuals on this page</h3>
          <p style={{ margin: "0 0 1rem 0", fontSize: "0.875rem" }}>
            Add a chart or KPI visual to start building your analytics dashboard.
          </p>
          <button
            onClick={handleAddSampleVisual}
            className="btn btn-primary"
            style={{ fontSize: "0.875rem", padding: "6px 14px" }}
          >
            + Add Visual
          </button>
        </div>
      ) : (
        <LayoutGrid
          width={page.canvasWidth}
          height={page.canvasHeight}
          scale={effectiveScale}
          displayOption={displayOption}
        >
          {page.visuals.map((visual) => (
            <VisualLayoutEditor
              key={visual.name}
              pageName={page.name}
              visual={visual}
              canvasWidth={page.canvasWidth}
              canvasHeight={page.canvasHeight}
              scale={effectiveScale}
              isActive={activeVisual === visual.name}
              onActivate={() => setActiveVisual(visual.name)}
            />
          ))}
        </LayoutGrid>
      )}
    </div>
  );
};
