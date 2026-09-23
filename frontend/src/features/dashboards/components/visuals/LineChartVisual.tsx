import React from "react";
import type { Visual } from "@entities/visual/types";
import { cleanFieldLabel, isValidMeasureName, validateVisualRoles } from "@entities/measure";

interface LineChartVisualProps {
  visual: Visual;
}

export const LineChartVisual: React.FC<LineChartVisualProps> = ({ visual }) => {
  const roleValidation = validateVisualRoles(visual.visualType, visual.boundFields);

  if (!roleValidation.isValid) {
    return (
      <div
        data-testid={`linechart-error-${visual.name}`}
        style={{
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          alignItems: "center",
          height: "100%",
          padding: "1rem",
          backgroundColor: "var(--danger-bg, #fef2f2)",
          border: "1px dashed var(--danger, #ef4444)",
          borderRadius: "var(--radius-sm, 6px)",
          boxSizing: "border-box",
          textAlign: "center"
        }}
      >
        <span style={{ fontSize: "1.25rem", color: "var(--danger, #ef4444)" }}>⚠️</span>
        <div style={{ fontWeight: 700, fontSize: "0.8rem", color: "var(--danger, #b91c1c)", marginTop: "0.25rem" }}>
          Invalid Trendline Measure Binding
        </div>
        <p style={{ fontSize: "0.7rem", color: "var(--text-secondary, #6b7280)", margin: "0.25rem 0 0 0" }}>
          {roleValidation.error}
        </p>
      </div>
    );
  }

  const boundFields = visual.boundFields;
  let xField: string;
  let yField: string;

  // Intelligent variable orientation: If only one field is bound and it's a measure,
  // it should be treated as the Y metric, projecting over a default Timeline dimension.
  if (boundFields.length === 1 && isValidMeasureName(cleanFieldLabel(boundFields[0]))) {
    yField = boundFields[0];
    xField = "Timeline";
  } else {
    xField = boundFields[0] ?? "Timeline";
    yField = boundFields[1] ?? "Metric";
  }

  const cleanX = cleanFieldLabel(xField);
  const cleanY = cleanFieldLabel(yField);

  const isArea = visual.visualType === "areaChart";

  // Cartesian Plot Geometry
  const plotLeft = 45;
  const plotRight = 300;
  const plotTop = 15;
  const plotBottom = 135;
  const plotWidth = plotRight - plotLeft;
  const plotHeight = plotBottom - plotTop;

  // Relative values between 0.0 and 1.0 (with basis at 0.0 = plotBottom)
  const normalizedValues = [0.25, 0.42, 0.68, 0.58, 0.85, 0.92];

  // Derive X categories / intervals
  const xLabels = React.useMemo(() => {
    const lowerX = cleanX.toLowerCase();
    if (lowerX.includes("month") || lowerX.includes("date") || lowerX === "timeline") {
      return ["T1", "T2", "T3", "T4", "T5", "T6"];
    }
    if (lowerX.includes("class") || lowerX.includes("pclass")) {
      return ["1st", "2nd", "3rd", "Deck A", "Deck B", "Deck C"];
    }
    if (lowerX.includes("sex") || lowerX.includes("gender")) {
      return ["All", "Male", "Female", "Adult", "Youth", "Senior"];
    }
    return ["P1", "P2", "P3", "P4", "P5", "P6"];
  }, [cleanX]);

  // Derive Y scale tick labels based on metric type
  const yTicks = React.useMemo(() => {
    const lowerY = cleanY.toLowerCase();
    if (lowerY.includes("rate") || lowerY.includes("pct") || lowerY.includes("percent")) {
      return ["100%", "75%", "50%", "25%", "0%"];
    }
    if (lowerY.includes("revenue") || lowerY.includes("sales") || lowerY.includes("amount") || lowerY.includes("fare")) {
      return ["$100k", "$75k", "$50k", "$25k", "$0"];
    }
    if (lowerY.includes("rows") || lowerY.includes("count") || lowerY.includes("total")) {
      return ["1k", "750", "500", "250", "0"];
    }
    return ["100", "75", "50", "25", "0"];
  }, [cleanY]);

  // Compute SVG coordinates for the points
  const points = normalizedValues.map((val, idx) => {
    const x = plotLeft + idx * (plotWidth / (normalizedValues.length - 1));
    const y = plotBottom - val * plotHeight;
    return { x, y, label: xLabels[idx] ?? `P${idx + 1}` };
  });

  const svgPoints = points.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(" ");
  const areaPoints = `${plotLeft},${plotBottom} ${svgPoints} ${plotRight},${plotBottom}`;

  // Gridline Y positions (100%, 75%, 50%, 25%, 0% basis)
  const gridLevels = [0, 0.25, 0.5, 0.75, 1.0];

  const titleText = xField === "Timeline" && boundFields.length === 1
    ? `${cleanY} over Timeline`
    : `${cleanY} by ${cleanX}`;

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
      <div style={{ marginBottom: "0.25rem", flexShrink: 0 }}>
        <span style={{ fontSize: "0.8125rem", fontWeight: 700, color: "var(--text-primary, #111827)" }}>
          {titleText}
        </span>
        <p style={{ margin: 0, fontSize: "0.6875rem", color: "var(--text-muted, #9ca3af)" }}>
          {isArea ? "Area Distribution Trendline" : "Continuous Line Trend"}
        </p>
      </div>

      <div
        style={{
          flex: 1,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          minHeight: 0,
          position: "relative"
        }}
      >
        <svg
          data-testid="linechart-svg"
          width="100%"
          height="100%"
          viewBox="0 0 320 170"
          preserveAspectRatio="xMidYMid meet"
          style={{ overflow: "visible", maxHeight: "100%" }}
        >
          <defs>
            <linearGradient id={`lineGrad-${visual.name}`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.35" />
              <stop offset="100%" stopColor="#3b82f6" stopOpacity="0.02" />
            </linearGradient>
          </defs>

          {/* Gridlines (Reference Levels) */}
          {gridLevels.map((lvl, i) => {
            const yPos = plotTop + lvl * plotHeight;
            return (
              <g key={`grid-${i}`}>
                <line
                  x1={plotLeft}
                  y1={yPos}
                  x2={plotRight}
                  y2={yPos}
                  stroke="var(--border-color, #e2e8f0)"
                  strokeWidth="1"
                  strokeDasharray={lvl === 1.0 ? "none" : "3 3"}
                />
                {/* Y-Axis Tick Value */}
                <text
                  x={plotLeft - 6}
                  y={yPos + 3}
                  textAnchor="end"
                  fontSize="7.5"
                  fill="var(--text-muted, #94a3b8)"
                  fontFamily="sans-serif"
                >
                  {yTicks[i]}
                </text>
              </g>
            );
          })}

          {/* Vertical Y-Axis Line */}
          <line
            data-testid="chart-y-axis"
            x1={plotLeft}
            y1={plotTop}
            x2={plotLeft}
            y2={plotBottom}
            stroke="var(--border-color, #cbd5e1)"
            strokeWidth="1.5"
          />

          {/* Horizontal Ground Basis Line (X-Axis) at Y = 0 */}
          <line
            data-testid="chart-basis-line"
            x1={plotLeft}
            y1={plotBottom}
            x2={plotRight}
            y2={plotBottom}
            stroke="var(--border-color, #94a3b8)"
            strokeWidth="1.5"
          />

          {/* X-Axis Tick Labels */}
          {points.map((p, idx) => (
            <text
              key={`xtick-${idx}`}
              x={p.x}
              y={plotBottom + 13}
              textAnchor="middle"
              fontSize="7.5"
              fill="var(--text-secondary, #64748b)"
              fontFamily="sans-serif"
            >
              {p.label}
            </text>
          ))}

          {/* X-Axis Category Variable Label */}
          <text
            x={(plotLeft + plotRight) / 2}
            y={plotBottom + 25}
            textAnchor="middle"
            fontSize="8"
            fontWeight="600"
            fill="var(--text-secondary, #475569)"
          >
            {cleanX}
          </text>

          {/* Y-Axis Metric Variable Label */}
          <text
            transform={`rotate(-90 12 ${(plotTop + plotBottom) / 2})`}
            x={12}
            y={(plotTop + plotBottom) / 2}
            textAnchor="middle"
            fontSize="8"
            fontWeight="600"
            fill="var(--text-secondary, #475569)"
          >
            {cleanY}
          </text>

          {/* Area Fill for AreaChart */}
          {isArea && <polygon points={areaPoints} fill={`url(#lineGrad-${visual.name})`} />}

          {/* Trend Polyline */}
          <polyline
            points={svgPoints}
            fill="none"
            stroke="#2563eb"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* Data Points on Polyline */}
          {points.map((p, idx) => (
            <circle
              key={idx}
              cx={p.x}
              cy={p.y}
              r="3.5"
              fill="#ffffff"
              stroke="#2563eb"
              strokeWidth="2"
            >
              <title>{`${p.label}: ${cleanY}`}</title>
            </circle>
          ))}
        </svg>
      </div>
    </div>
  );
};
