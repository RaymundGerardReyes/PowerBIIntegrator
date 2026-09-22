import React from "react";

export type CanvasDisplayOption = "FitToPage" | "FitToWidth" | "ActualSize";

export interface LayoutGridProps {
  width: number;
  height: number;
  scale?: number;
  displayOption?: CanvasDisplayOption;
  children: React.ReactNode;
}

export const LayoutGrid: React.FC<LayoutGridProps> = ({
  width,
  height,
  scale = 1,
  displayOption = "FitToPage",
  children
}) => {
  const scaledWidth = Math.round(width * scale);
  const scaledHeight = Math.round(height * scale);

  return (
    <div
      data-testid="canvas-viewport-container"
      style={{
        width: "100%",
        maxWidth: "100%",
        overflow: displayOption === "ActualSize" || scale > 1 ? "auto" : "hidden",
        backgroundColor: "var(--bg-canvas-outer, #f1f5f9)",
        padding: "1rem",
        borderRadius: "8px",
        boxSizing: "border-box",
        display: "flex",
        justifyContent: "center",
        alignItems: "flex-start",
        minHeight: "400px"
      }}
    >
      {/* Spacer container matching scaled dimensions to establish correct flow */}
      <div
        data-testid="canvas-scaler-wrapper"
        style={{
          width: scaledWidth,
          height: scaledHeight,
          position: "relative",
          flexShrink: 0
        }}
      >
        <div
          data-testid="layout-grid-canvas"
          style={{
            position: "absolute",
            top: 0,
            left: 0,
            width,
            height,
            transform: `scale(${scale})`,
            transformOrigin: "top left",
            backgroundColor: "var(--bg-card, #ffffff)",
            border: "1px solid var(--border-color, #cbd5e1)",
            borderRadius: "6px",
            boxShadow: "0 10px 15px -3px rgba(0, 0, 0, 0.08), 0 4px 6px -2px rgba(0, 0, 0, 0.04)",
            boxSizing: "border-box"
          }}
        >
          {children}
        </div>
      </div>
    </div>
  );
};
