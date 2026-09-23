import React from "react";
import type { Visual } from "@entities/visual/types";
import { cleanFieldLabel, isValidMeasureName } from "@entities/measure";
import { useDashboardStore } from "../model/dashboardSlice";
import { useLayoutEditor } from "../hooks/useLayoutEditor";
import { CardVisual } from "./visuals/CardVisual";
import { BarChartVisual } from "./visuals/BarChartVisual";
import { LineChartVisual } from "./visuals/LineChartVisual";
import { DonutChartVisual } from "./visuals/DonutChartVisual";
import { TableVisual } from "./visuals/TableVisual";

export const RECOMMENDED_VISUAL_DIMENSIONS: Record<string, { minW: number; minH: number; recW: number; recH: number }> = {
  card: { minW: 180, minH: 120, recW: 300, recH: 160 },
  barChart: { minW: 260, minH: 220, recW: 580, recH: 320 },
  columnChart: { minW: 260, minH: 220, recW: 580, recH: 320 },
  lineChart: { minW: 260, minH: 200, recW: 580, recH: 300 },
  areaChart: { minW: 260, minH: 200, recW: 580, recH: 300 },
  donutChart: { minW: 240, minH: 220, recW: 380, recH: 260 },
  pieChart: { minW: 240, minH: 220, recW: 380, recH: 260 },
  table: { minW: 300, minH: 220, recW: 600, recH: 340 },
  tableEx: { minW: 300, minH: 220, recW: 600, recH: 340 },
  matrix: { minW: 300, minH: 220, recW: 600, recH: 340 }
};

interface VisualLayoutEditorProps {
  pageName: string;
  visual: Visual;
  canvasWidth?: number;
  canvasHeight?: number;
  isActive?: boolean;
  onActivate?: () => void;
}

export const VisualLayoutEditor: React.FC<VisualLayoutEditorProps> = ({
  pageName,
  visual,
  canvasWidth = 1280,
  canvasHeight = 720,
  isActive = false,
  onActivate
}) => {
  const { updateVisualLayout, updateVisualType, updateVisualBoundField } = useLayoutEditor();
  const currentDashboard = useDashboardStore((s) => s.current);
  const moveVisualToPage = useDashboardStore((s) => s.moveVisualToPage);

  const availableFields = React.useMemo<string[]>(() => {
    if (!currentDashboard) return visual.boundFields;
    const all: string[] = currentDashboard.pages.flatMap((p) => p.visuals.flatMap((v) => v.boundFields));
    const unique: string[] = Array.from(new Set(all));
    return unique.length > 0 ? unique : visual.boundFields;
  }, [currentDashboard, visual.boundFields]);

  const { measures, dimensions } = React.useMemo(() => {
    const meas: string[] = [];
    const dims: string[] = [];
    for (const f of availableFields) {
      const clean = cleanFieldLabel(f);
      if (isValidMeasureName(clean)) {
        meas.push(f);
      } else {
        dims.push(f);
      }
    }
    return { measures: meas, dimensions: dims };
  }, [availableFields]);

  // Modern size-aware classification based on inline width
  const isMicro = visual.layout.width < 240;
  const isCompact = visual.layout.width >= 240 && visual.layout.width < 380;
  const isMultiAxis =
    visual.visualType !== "table" &&
    visual.visualType !== "tableEx" &&
    visual.visualType !== "card";

  const getVisualTypeIcon = (type: string) => {
    switch (type) {
      case "card": return "📊";
      case "barChart": return "📉";
      case "columnChart": return "📊";
      case "lineChart": return "📈";
      case "areaChart": return "📉";
      case "donutChart": return "🍩";
      case "pieChart": return "🥧";
      case "table":
      case "tableEx":
      case "matrix": return "📋";
      default: return "📦";
    }
  };

  const renderVisualContent = () => {
    switch (visual.visualType) {
      case "card":
        return <CardVisual visual={visual} />;
      case "barChart":
      case "columnChart":
        return <BarChartVisual visual={visual} />;
      case "lineChart":
      case "areaChart":
        return <LineChartVisual visual={visual} />;
      case "donutChart":
      case "pieChart":
        return <DonutChartVisual visual={visual} />;
      case "table":
      case "tableEx":
      case "matrix":
        return <TableVisual visual={visual} />;
      default:
        return <BarChartVisual visual={visual} />;
    }
  };

  const handleTypeChange = (newType: string) => {
    updateVisualType(pageName, visual.name, newType);
    const rec = RECOMMENDED_VISUAL_DIMENSIONS[newType];
    if (rec) {
      const currentW = visual.layout.width;
      const currentH = visual.layout.height;
      let targetW = currentW;
      let targetH = currentH;

      if (currentH < rec.minH) {
        targetH = rec.recH;
      }
      if (currentW < rec.minW) {
        targetW = rec.recW;
      }

      if (targetW !== currentW || targetH !== currentH) {
        const maxX = canvasWidth - visual.layout.x;
        const maxY = canvasHeight - visual.layout.y;
        const clampedW = Math.max(rec.minW, Math.min(maxX, targetW));
        const clampedH = Math.max(rec.minH, Math.min(maxY, targetH));
        updateVisualLayout(pageName, visual.name, {
          width: clampedW,
          height: clampedH
        });
      }
    }
  };

  const handleNudgePosition = (e: React.MouseEvent) => {
    e.stopPropagation();
    const maxX = Math.max(0, canvasWidth - visual.layout.width);
    const newX = Math.min(maxX, visual.layout.x + 10);
    updateVisualLayout(pageName, visual.name, { x: newX });
  };

  return (
    <div
      data-testid={`visual-${visual.name}`}
      onClick={onActivate}
      style={{
        position: "absolute",
        left: visual.layout.x,
        top: visual.layout.y,
        width: Math.max(180, visual.layout.width),
        height: Math.max(120, visual.layout.height),
        zIndex: isActive ? 20 : (visual.layout.z ?? 1),
        containerType: "inline-size",
        containerName: "visual-card",
        backgroundColor: "var(--bg-card, #ffffff)",
        border: isActive ? "2px solid #2563eb" : "1px solid var(--border-color, #e5e7eb)",
        borderRadius: "var(--radius-md, 8px)",
        boxShadow: isActive
          ? "0 4px 12px rgba(37, 99, 235, 0.2)"
          : "var(--shadow-sm, 0 1px 2px 0 rgba(0, 0, 0, 0.05))",
        padding: isMicro ? "0.4rem" : "0.625rem",
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
        transition: "box-shadow 0.15s ease, border-color 0.15s ease",
        boxSizing: "border-box",
        overflow: "hidden"
      }}
    >
      {/* Visual Header & Controls with Size-Aware Responsive Adaptations */}
      <div
        data-testid={`visual-header-${visual.name}`}
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          borderBottom: "1px solid var(--border-color, #f3f4f6)",
          paddingBottom: isMicro ? "0.2rem" : "0.35rem",
          marginBottom: isMicro ? "0.2rem" : "0.35rem",
          gap: "0.35rem",
          flexWrap: isMicro ? "wrap" : "nowrap"
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "0.35rem", minWidth: 0, flex: 1 }}>
          <span style={{ fontSize: "0.875rem" }}>{getVisualTypeIcon(visual.visualType)}</span>
          <span
            style={{
              fontWeight: 600,
              fontSize: isMicro ? "0.75rem" : "0.8125rem",
              color: "var(--text-primary, #111827)",
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
              maxWidth: isMicro ? "90px" : isCompact ? "120px" : "200px"
            }}
            title={visual.name}
          >
            {visual.name}
          </span>
        </div>

        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: "0.25rem",
            flexShrink: 0,
            flexWrap: isMicro ? "wrap" : "nowrap"
          }}
        >
          {/* Primary Field Selector (Slot 0) */}
          {availableFields.length > 0 && visual.visualType !== "table" && (
            <select
              value={visual.boundFields[0] ?? ""}
              onChange={(e) => updateVisualBoundField(pageName, visual.name, 0, e.target.value)}
              aria-label={`select-field-${visual.name}`}
              style={{
                fontSize: "0.6875rem",
                padding: "2px 4px",
                borderRadius: "4px",
                border: "1px solid var(--border-color, #d1d5db)",
                backgroundColor: "var(--bg-subtle, #f9fafb)",
                color: "var(--text-secondary, #374151)",
                cursor: "pointer",
                maxWidth: isMicro ? "80px" : "105px",
                textOverflow: "ellipsis"
              }}
              title={visual.visualType === "card" ? "Select DAX Measure" : "Select Category / Dimension"}
            >
              {visual.visualType === "card" ? (
                <>
                  <optgroup label="DAX Measures (Valid)">
                    {measures.map((f) => (
                      <option key={f} value={f}>
                        {cleanFieldLabel(f)}
                      </option>
                    ))}
                  </optgroup>
                  {dimensions.length > 0 && (
                    <optgroup label="Raw Columns (Invalid)">
                      {dimensions.map((f) => (
                        <option key={f} value={f}>
                          {cleanFieldLabel(f)}
                        </option>
                      ))}
                    </optgroup>
                  )}
                </>
              ) : (
                <>
                  {dimensions.length > 0 && (
                    <optgroup label="Dimensions (Category)">
                      {dimensions.map((f) => (
                        <option key={f} value={f}>
                          {cleanFieldLabel(f)}
                        </option>
                      ))}
                    </optgroup>
                  )}
                  {measures.length > 0 && (
                    <optgroup label="Measures">
                      {measures.map((f) => (
                        <option key={f} value={f}>
                          {cleanFieldLabel(f)}
                        </option>
                      ))}
                    </optgroup>
                  )}
                </>
              )}
            </select>
          )}

          {/* Secondary Metric/Measure Selector for 2-Slot Charts (Slot 1) */}
          {availableFields.length > 0 && isMultiAxis && (
            <select
              value={visual.boundFields[1] ?? ""}
              onChange={(e) => updateVisualBoundField(pageName, visual.name, 1, e.target.value)}
              aria-label={`select-measure-${visual.name}`}
              style={{
                fontSize: "0.6875rem",
                padding: "2px 4px",
                borderRadius: "4px",
                border: "1px solid var(--border-color, #d1d5db)",
                backgroundColor: "var(--bg-subtle, #f9fafb)",
                color: "var(--text-secondary, #374151)",
                cursor: "pointer",
                maxWidth: isMicro ? "75px" : "95px",
                textOverflow: "ellipsis"
              }}
              title="Select DAX Measure (Y / Value axis)"
            >
              <option value="" disabled>Select metric...</option>
              {measures.length > 0 && (
                <optgroup label="DAX Measures">
                  {measures.map((f) => (
                    <option key={f} value={f}>
                      {cleanFieldLabel(f)}
                    </option>
                  ))}
                </optgroup>
              )}
              {dimensions.length > 0 && (
                <optgroup label="Raw Columns (Invalid)">
                  {dimensions.map((f) => (
                    <option key={f} value={f}>
                      {cleanFieldLabel(f)}
                    </option>
                  ))}
                </optgroup>
              )}
            </select>
          )}

          {/* Live Type Customization Dropdown */}
          <select
            value={visual.visualType}
            onChange={(e) => handleTypeChange(e.target.value)}
            aria-label={`select-type-${visual.name}`}
            style={{
              fontSize: "0.6875rem",
              padding: "2px 4px",
              borderRadius: "4px",
              border: "1px solid var(--border-color, #d1d5db)",
              backgroundColor: "var(--bg-subtle, #f9fafb)",
              color: "var(--text-secondary, #374151)",
              cursor: "pointer"
            }}
          >
            <option value="card">KPI Card</option>
            <option value="barChart">Bar Chart</option>
            <option value="columnChart">Column Chart</option>
            <option value="lineChart">Line Chart</option>
            <option value="areaChart">Area Chart</option>
            <option value="donutChart">Donut Chart</option>
            <option value="pieChart">Pie Chart</option>
            <option value="table">Table Grid</option>
          </select>
        </div>
      </div>

      {/* Main Graphical Chart Body with Overflow Guard */}
      <div style={{ flex: 1, minHeight: 0, overflow: "hidden" }}>
        {renderVisualContent()}
      </div>

      {/* Visual Footer & Layout Controls */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          borderTop: "1px solid var(--border-color, #f1f5f9)",
          paddingTop: "0.25rem",
          marginTop: "0.25rem",
          fontSize: "0.6875rem",
          gap: "0.25rem"
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "0.4rem", overflow: "hidden", minWidth: 0, flex: 1 }}>
          <span style={{ color: "var(--text-muted, #94a3b8)", fontFamily: "monospace", fontSize: "0.65rem", flexShrink: 0 }}>
            ⤢ {visual.layout.x}, {visual.layout.y}
          </span>
          {visual.boundFields[0] && (
            <span
              style={{
                fontSize: "0.625rem",
                padding: "1px 6px",
                borderRadius: "3px",
                backgroundColor: "var(--bg-subtle, #f8fafc)",
                border: "1px solid var(--border-color, #e2e8f0)",
                color: "var(--text-secondary, #475569)",
                fontFamily: "monospace",
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
                maxWidth: isMicro ? "70px" : isCompact ? "100px" : "150px"
              }}
              title={visual.boundFields[0]}
            >
              {visual.boundFields[0].replace(/T_\d+_([a-zA-Z0-9]+)_xls/gi, "$1")}
            </span>
          )}
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: "0.25rem", flexShrink: 0 }}>
          {currentDashboard && currentDashboard.pages.length > 1 && (
            <select
              value=""
              onChange={(e) => {
                if (e.target.value) {
                  moveVisualToPage(pageName, e.target.value, visual.name);
                }
              }}
              aria-label={`move-to-page-${visual.name}`}
              style={{
                fontSize: "0.625rem",
                padding: "1px 4px",
                borderRadius: "3px",
                border: "1px solid var(--border-color, #d1d5db)",
                backgroundColor: "var(--bg-subtle, #f8fafc)",
                color: "var(--text-secondary, #64748b)",
                cursor: "pointer",
                maxWidth: isMicro ? "65px" : "85px"
              }}
              title="Move this visual to another report page"
            >
              <option value="" disabled>Move ➡️</option>
              {currentDashboard.pages
                .filter((p) => p.name !== pageName)
                .map((p) => (
                  <option key={p.name} value={p.name}>
                    To {p.name}
                  </option>
                ))}
            </select>
          )}

          <button
            onClick={handleNudgePosition}
            aria-label={`move-${visual.name}`}
            className="btn btn-secondary btn-sm"
            style={{
              fontSize: "0.65rem",
              padding: "1px 6px",
              lineHeight: 1.4,
              borderRadius: "3px",
              color: "var(--text-secondary, #64748b)"
            }}
            title="Nudge visual position by +10px X (clamped at canvas border)"
          >
            ⇄ Move
          </button>
        </div>
      </div>
    </div>
  );
};
