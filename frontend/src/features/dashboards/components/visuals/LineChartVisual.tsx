import React from "react";
import type { Visual } from "@entities/visual/types";

interface LineChartVisualProps {
  visual: Visual;
}

export const LineChartVisual: React.FC<LineChartVisualProps> = ({ visual }) => {
  const boundFields = visual.boundFields;
  const xField = boundFields[0] ?? "Timeline";
  const yField = boundFields[1] ?? "Metric";

  const cleanX = xField.includes("[") ? xField.substring(xField.indexOf("[") + 1).replace("]", "") : xField;
  const cleanY = yField.includes("[") ? yField.substring(yField.indexOf("[") + 1).replace("]", "") : yField;

  // Sample data points for trend/distribution
  const points = [
    { x: 10, y: 70 },
    { x: 30, y: 55 },
    { x: 50, y: 35 },
    { x: 70, y: 45 },
    { x: 90, y: 20 },
    { x: 110, y: 15 }
  ];

  const svgPoints = points.map((p) => `${p.x * 2},${p.y * 1.5}`).join(" ");
  const areaPoints = `${points[0].x * 2},120 ${svgPoints} ${points[points.length - 1].x * 2},120`;

  const isArea = visual.visualType === "areaChart";

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        padding: "0.5rem",
        overflow: "hidden",
        boxSizing: "border-box"
      }}
    >
      <div style={{ marginBottom: "0.25rem" }}>
        <span style={{ fontSize: "0.8125rem", fontWeight: 700, color: "var(--text-primary, #111827)" }}>
          {cleanY} by {cleanX}
        </span>
        <p style={{ margin: 0, fontSize: "0.6875rem", color: "var(--text-muted, #9ca3af)" }}>
          {isArea ? "Area Distribution Trendline" : "Continuous Line Trend"}
        </p>
      </div>

      <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", minHeight: 0 }}>
        <svg width="100%" height="90" viewBox="0 0 240 120" style={{ overflow: "visible" }}>
          <defs>
            <linearGradient id="lineGrad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.3" />
              <stop offset="100%" stopColor="#3b82f6" stopOpacity="0.0" />
            </linearGradient>
          </defs>

          {/* Area Fill only for AreaChart */}
          {isArea && <polygon points={areaPoints} fill="url(#lineGrad)" />}

          {/* Polyline */}
          <polyline
            points={svgPoints}
            fill="none"
            stroke="#3b82f6"
            strokeWidth="3"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* Data Points */}
          {points.map((p, idx) => (
            <circle
              key={idx}
              cx={p.x * 2}
              cy={p.y * 1.5}
              r="4"
              fill="#ffffff"
              stroke="#3b82f6"
              strokeWidth="2"
            />
          ))}
        </svg>
      </div>
    </div>
  );
};

