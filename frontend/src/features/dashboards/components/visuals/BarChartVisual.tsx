import React from "react";
import type { Visual } from "@entities/visual/types";
import { cleanFieldLabel, validateVisualRoles } from "@entities/measure";

interface BarChartVisualProps {
  visual: Visual;
}

interface BarItem {
  label: string;
  count: string;
  percent: number;
  color: string;
}

function getDistributionData(category: string, measure?: string): BarItem[] {
  const cat = category.toLowerCase();
  const meas = (measure ?? "").toLowerCase();

  const isRate = meas.includes("rate") || meas.includes("pct") || meas.includes("percent");
  const isCurrency = meas.includes("fare") || meas.includes("price") || meas.includes("amount") || meas.includes("sales") || meas.includes("revenue");

  if (cat === "pclass" || cat === "class" || cat === "tier") {
    if (isRate) {
      return [
        { label: "1st Class", count: "62.0%", percent: 62, color: "#2563eb" },
        { label: "2nd Class", count: "47.3%", percent: 47, color: "#10b981" },
        { label: "3rd Class", count: "24.2%", percent: 24, color: "#f59e0b" }
      ];
    }
    if (isCurrency) {
      return [
        { label: "1st Class", count: "$28,262", percent: 65, color: "#2563eb" },
        { label: "3rd Class", count: "$9,418", percent: 21, color: "#f59e0b" },
        { label: "2nd Class", count: "$5,872", percent: 14, color: "#10b981" }
      ];
    }
    return [
      { label: "3rd Class", count: "709", percent: 54, color: "#f59e0b" },
      { label: "1st Class", count: "323", percent: 25, color: "#2563eb" },
      { label: "2nd Class", count: "277", percent: 21, color: "#10b981" }
    ];
  }

  if (cat === "sex" || cat === "gender") {
    if (isRate) {
      return [
        { label: "Female", count: "74.2%", percent: 74, color: "#ec4899" },
        { label: "Male", count: "18.9%", percent: 19, color: "#2563eb" }
      ];
    }
    if (isCurrency) {
      return [
        { label: "Male", count: "$22,015", percent: 51, color: "#2563eb" },
        { label: "Female", count: "$21,537", percent: 49, color: "#ec4899" }
      ];
    }
    return [
      { label: "Male", count: "843", percent: 64, color: "#2563eb" },
      { label: "Female", count: "466", percent: 36, color: "#ec4899" }
    ];
  }

  if (cat === "embarked" || cat === "port") {
    if (isRate) {
      return [
        { label: "Cherbourg", count: "55.6%", percent: 56, color: "#10b981" },
        { label: "Queenstown", count: "38.9%", percent: 39, color: "#f59e0b" },
        { label: "Southampton", count: "33.7%", percent: 34, color: "#2563eb" }
      ];
    }
    if (isCurrency) {
      return [
        { label: "Southampton", count: "$25,188", percent: 58, color: "#2563eb" },
        { label: "Cherbourg", count: "$15,082", percent: 35, color: "#10b981" },
        { label: "Queenstown", count: "$3,282", percent: 7, color: "#f59e0b" }
      ];
    }
    return [
      { label: "Southampton", count: "914", percent: 70, color: "#2563eb" },
      { label: "Cherbourg", count: "270", percent: 21, color: "#10b981" },
      { label: "Queenstown", count: "125", percent: 9, color: "#f59e0b" }
    ];
  }

  if (cat.includes("age")) {
    if (isRate) {
      return [
        { label: "< 18 yrs", count: "54.0%", percent: 54, color: "#f59e0b" },
        { label: "18-35 yrs", count: "38.0%", percent: 38, color: "#2563eb" },
        { label: "36-50 yrs", count: "42.0%", percent: 42, color: "#10b981" },
        { label: "50+ yrs", count: "36.0%", percent: 36, color: "#8b5cf6" }
      ];
    }
    return [
      { label: "18-35 yrs", count: "624", percent: 48, color: "#2563eb" },
      { label: "36-50 yrs", count: "338", percent: 26, color: "#10b981" },
      { label: "< 18 yrs", count: "195", percent: 15, color: "#f59e0b" },
      { label: "50+ yrs", count: "152", percent: 11, color: "#8b5cf6" }
    ];
  }

  if (isRate) {
    return [
      { label: `${category} Alpha`, count: "65.0%", percent: 65, color: "#2563eb" },
      { label: `${category} Beta`, count: "42.0%", percent: 42, color: "#10b981" },
      { label: `${category} Gamma`, count: "28.0%", percent: 28, color: "#f59e0b" }
    ];
  }

  if (isCurrency) {
    return [
      { label: `${category} Alpha`, count: "$18,400", percent: 45, color: "#2563eb" },
      { label: `${category} Beta`, count: "$13,100", percent: 32, color: "#10b981" },
      { label: `${category} Gamma`, count: "$9,400", percent: 23, color: "#f59e0b" }
    ];
  }

  return [
    { label: `${category} Alpha`, count: "589", percent: 45, color: "#2563eb" },
    { label: `${category} Beta`, count: "419", percent: 32, color: "#10b981" },
    { label: `${category} Gamma`, count: "301", percent: 23, color: "#f59e0b" }
  ];
}

export const BarChartVisual: React.FC<BarChartVisualProps> = ({ visual }) => {
  const roleValidation = validateVisualRoles(visual.visualType, visual.boundFields);

  if (!roleValidation.isValid) {
    return (
      <div
        data-testid={`barchart-error-${visual.name}`}
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
          Invalid Chart Measure Binding
        </div>
        <p style={{ fontSize: "0.7rem", color: "var(--text-secondary, #6b7280)", margin: "0.25rem 0 0 0" }}>
          {roleValidation.error}
        </p>
      </div>
    );
  }

  const categoryField = visual.boundFields[0] ?? "Category";
  const measureField = visual.boundFields[1] ?? "Count";

  const cleanCategory = cleanFieldLabel(categoryField);
  const cleanMeasure = cleanFieldLabel(measureField);

  const friendlyCategory = cleanCategory.charAt(0).toUpperCase() + cleanCategory.slice(1);
  const friendlyMeasure = cleanMeasure.replace(/_/g, " ");

  const isColumnChart = visual.visualType === "columnChart";
  const data = getDistributionData(cleanCategory, cleanMeasure);

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
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "0.35rem" }}>
        <div>
          <span style={{ fontSize: "0.8125rem", fontWeight: 700, color: "var(--text-primary, #111827)" }}>
            {friendlyMeasure} by {friendlyCategory}
          </span>
          <p style={{ margin: 0, fontSize: "0.6875rem", color: "var(--text-muted, #9ca3af)" }}>
            {isColumnChart ? "Vertical Column Distribution" : "Horizontal Bar Breakdown"} • DAX Aggregation
          </p>
        </div>
      </div>

      {roleValidation.warning && (
        <div
          data-testid={`barchart-warning-${visual.name}`}
          style={{
            fontSize: "0.65rem",
            backgroundColor: "var(--warning-bg, #fefce8)",
            color: "var(--warning-text, #a16207)",
            border: "1px solid var(--warning-border, #fef08a)",
            borderRadius: "4px",
            padding: "2px 6px",
            marginBottom: "0.35rem"
          }}
        >
          ⚠️ {roleValidation.warning}
        </div>
      )}

      {isColumnChart ? (
        /* Vertical Column Chart */
        <div
          style={{
            flex: 1,
            display: "flex",
            alignItems: "flex-end",
            justifyContent: "space-around",
            gap: "0.5rem",
            padding: "0.5rem 0.25rem 0 0.25rem",
            borderBottom: "2px solid var(--border-color, #e2e8f0)",
            minHeight: 0
          }}
        >
          {data.map((item) => (
            <div
              key={item.label}
              style={{
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                height: "100%",
                justifyContent: "flex-end",
                flex: 1,
                minWidth: 0
              }}
            >
              <span style={{ fontSize: "0.6875rem", fontWeight: 700, color: "var(--text-primary, #111827)", marginBottom: "3px" }}>
                {item.count}
              </span>
              <div
                style={{
                  width: "100%",
                  maxWidth: "48px",
                  height: `${Math.max(item.percent * 1.5, 16)}px`,
                  maxHeight: "75%",
                  backgroundColor: item.color,
                  borderRadius: "4px 4px 0 0",
                  transition: "height 0.4s ease-out"
                }}
              />
              <span
                style={{
                  fontSize: "0.6875rem",
                  color: "var(--text-secondary, #4b5563)",
                  fontWeight: 600,
                  marginTop: "6px",
                  whiteSpace: "nowrap",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  maxWidth: "85px"
                }}
                title={item.label}
              >
                {item.label}
              </span>
            </div>
          ))}
        </div>
      ) : (
        /* Horizontal Bar Chart */
        <div style={{ flex: 1, display: "flex", flexDirection: "column", justifyContent: "space-around", gap: "0.35rem" }}>
          {data.map((item) => (
            <div key={item.label} style={{ display: "flex", flexDirection: "column", gap: "2px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", fontSize: "0.75rem", color: "var(--text-secondary, #4b5563)" }}>
                <span style={{ fontWeight: 600 }}>{item.label}</span>
                <div style={{ display: "flex", gap: "0.5rem", alignItems: "baseline" }}>
                  <span style={{ fontSize: "0.6875rem", color: "var(--text-muted, #9ca3af)" }}>
                    {item.count}
                  </span>
                  <span style={{ fontFamily: "monospace", fontWeight: 700, color: "var(--text-primary, #111827)" }}>
                    {item.percent}%
                  </span>
                </div>
              </div>
              <div
                style={{
                  width: "100%",
                  height: "12px",
                  backgroundColor: "var(--bg-subtle, #f1f5f9)",
                  borderRadius: "999px",
                  overflow: "hidden"
                }}
              >
                <div
                  style={{
                    width: `${item.percent}%`,
                    height: "100%",
                    backgroundColor: item.color,
                    borderRadius: "999px",
                    transition: "width 0.4s ease-out"
                  }}
                />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
