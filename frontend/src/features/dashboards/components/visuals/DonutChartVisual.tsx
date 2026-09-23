import React from "react";
import type { Visual } from "@entities/visual/types";
import { cleanFieldLabel, validateVisualRoles } from "@entities/measure";

interface DonutChartVisualProps {
  visual: Visual;
}

interface DonutSegment {
  label: string;
  percent: number;
  color: string;
}

function getDonutSegments(category: string): DonutSegment[] {
  const cat = category.toLowerCase();

  if (cat === "sex" || cat === "gender") {
    return [
      { label: "Male", percent: 64, color: "#2563eb" },
      { label: "Female", percent: 36, color: "#ec4899" }
    ];
  }

  if (cat === "pclass" || cat === "class" || cat === "tier") {
    return [
      { label: "3rd Class", percent: 54, color: "#f59e0b" },
      { label: "1st Class", percent: 25, color: "#2563eb" },
      { label: "2nd Class", percent: 21, color: "#10b981" }
    ];
  }

  if (cat === "embarked" || cat === "port") {
    return [
      { label: "Southampton", percent: 70, color: "#2563eb" },
      { label: "Cherbourg", percent: 21, color: "#10b981" },
      { label: "Queenstown", percent: 9, color: "#f59e0b" }
    ];
  }

  if (cat === "survived" || cat === "target" || cat === "churn") {
    return [
      { label: "Not Survived", percent: 62, color: "#64748b" },
      { label: "Survived", percent: 38, color: "#10b981" }
    ];
  }

  if (cat.includes("age")) {
    return [
      { label: "18-35 yrs", percent: 48, color: "#2563eb" },
      { label: "36-50 yrs", percent: 26, color: "#10b981" },
      { label: "< 18 yrs", percent: 15, color: "#f59e0b" },
      { label: "50+ yrs", percent: 11, color: "#8b5cf6" }
    ];
  }

  if (cat.includes("fare") || cat.includes("price") || cat.includes("amount")) {
    return [
      { label: "Economy", percent: 54, color: "#2563eb" },
      { label: "Business", percent: 30, color: "#10b981" },
      { label: "First", percent: 16, color: "#f59e0b" }
    ];
  }

  if (cat === "region" || cat === "country") {
    return [
      { label: "North America", percent: 44, color: "#2563eb" },
      { label: "Europe", percent: 33, color: "#10b981" },
      { label: "Asia-Pacific", percent: 23, color: "#f59e0b" }
    ];
  }

  if (cat === "status" || cat === "state") {
    return [
      { label: "Active", percent: 56, color: "#10b981" },
      { label: "Pending", percent: 28, color: "#f59e0b" },
      { label: "Closed", percent: 16, color: "#64748b" }
    ];
  }

  return [
    { label: `${category} A`, percent: 48, color: "#2563eb" },
    { label: `${category} B`, percent: 32, color: "#10b981" },
    { label: `${category} C`, percent: 20, color: "#f59e0b" }
  ];
}

export const DonutChartVisual: React.FC<DonutChartVisualProps> = ({ visual }) => {
  const roleValidation = validateVisualRoles(visual.visualType, visual.boundFields);

  if (!roleValidation.isValid) {
    return (
      <div
        data-testid={`donutchart-error-${visual.name}`}
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
          Invalid Slice Measure Binding
        </div>
        <p style={{ fontSize: "0.7rem", color: "var(--text-secondary, #6b7280)", margin: "0.25rem 0 0 0" }}>
          {roleValidation.error}
        </p>
      </div>
    );
  }

  const categoryField = visual.boundFields[0] ?? "Proportions";
  const cleanCategory = cleanFieldLabel(categoryField);

  const friendlyCategory = cleanCategory.charAt(0).toUpperCase() + cleanCategory.slice(1);
  const segments = getDonutSegments(cleanCategory);

  const isPie = visual.visualType === "pieChart";
  const radius = isPie ? 25 : 42;
  const strokeWidth = isPie ? 50 : 16;
  const circumference = 2 * Math.PI * radius;

  let runningOffset = 0;
  const computedSegments = segments.map((seg) => {
    const strokeDasharray = `${(seg.percent / 100) * circumference} ${circumference}`;
    const strokeDashoffset = -((runningOffset / 100) * circumference);
    runningOffset += seg.percent;
    return { ...seg, strokeDasharray, strokeDashoffset };
  });

  const isCompactHeight = visual.layout.height < 220;

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        padding: isCompactHeight ? "0.25rem 0.4rem" : "0.5rem",
        overflow: "hidden",
        boxSizing: "border-box"
      }}
    >
      <div style={{ marginBottom: isCompactHeight ? "0.1rem" : "0.25rem", flexShrink: 0 }}>
        <span style={{ fontSize: isCompactHeight ? "0.75rem" : "0.8125rem", fontWeight: 700, color: "var(--text-primary, #111827)" }}>
          {isPie ? "Pie Chart" : "Proportions"} by {friendlyCategory}
        </span>
        {!isCompactHeight && (
          <p style={{ margin: 0, fontSize: "0.6875rem", color: "var(--text-muted, #9ca3af)" }}>
            {isPie ? "Full Proportional Slices" : "Ring Ratio Distribution"}
          </p>
        )}
      </div>

      <div style={{ flex: 1, minHeight: 0, display: "flex", alignItems: "center", justifyContent: "center", gap: isCompactHeight ? "0.5rem" : "1rem" }}>
        {/* SVG Donut / Pie with fluid container-aware scaling */}
        <div
          style={{
            position: "relative",
            height: "100%",
            maxHeight: isCompactHeight ? "85px" : "110px",
            aspectRatio: "1 / 1",
            flexShrink: 0,
            display: "flex",
            alignItems: "center",
            justifyContent: "center"
          }}
        >
          <svg
            data-testid="donutchart-svg"
            width="100%"
            height="100%"
            viewBox="0 0 110 110"
            preserveAspectRatio="xMidYMid meet"
            style={{ transform: "rotate(-90deg)", width: "100%", height: "100%" }}
          >
            <circle
              cx="55"
              cy="55"
              r={radius}
              fill="transparent"
              stroke="var(--bg-subtle, #f1f5f9)"
              strokeWidth={strokeWidth}
            />
            {computedSegments.map((seg) => (
              <circle
                key={seg.label}
                cx="55"
                cy="55"
                r={radius}
                fill="transparent"
                stroke={seg.color}
                strokeWidth={strokeWidth}
                strokeDasharray={seg.strokeDasharray}
                strokeDashoffset={seg.strokeDashoffset}
                strokeLinecap={isPie ? "butt" : "round"}
              />
            ))}
          </svg>
          {!isPie && (
            <div
              style={{
                position: "absolute",
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                fontSize: isCompactHeight ? "0.65rem" : "0.75rem",
                fontWeight: 700,
                color: "var(--text-primary, #111827)",
                pointerEvents: "none"
              }}
            >
              <span>100%</span>
              <span style={{ fontSize: isCompactHeight ? "0.5rem" : "0.6rem", color: "var(--text-muted, #9ca3af)", fontWeight: 400 }}>Total</span>
            </div>
          )}
        </div>

        {/* Legend */}
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            gap: isCompactHeight ? "0.15rem" : "0.35rem",
            fontSize: isCompactHeight ? "0.6875rem" : "0.75rem",
            minWidth: isCompactHeight ? "80px" : "95px",
            maxHeight: "100%",
            overflowY: "auto"
          }}
        >
          {segments.map((seg) => (
            <div key={seg.label} style={{ display: "flex", alignItems: "center", gap: "0.35rem" }}>
              <span style={{ width: "6px", height: "6px", borderRadius: "50%", backgroundColor: seg.color, flexShrink: 0 }} />
              <span style={{ color: "var(--text-secondary, #4b5563)", fontWeight: 500, fontSize: isCompactHeight ? "0.6875rem" : "0.75rem", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                {seg.label}
              </span>
              <span style={{ fontWeight: 700, color: "var(--text-primary, #111827)", marginLeft: "auto", fontFamily: "monospace", fontSize: isCompactHeight ? "0.6875rem" : "0.75rem" }}>
                {seg.percent}%
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
