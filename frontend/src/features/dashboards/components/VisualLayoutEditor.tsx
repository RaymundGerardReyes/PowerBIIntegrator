import React from "react";
import type { Visual } from "@entities/visual/types";
import { useLayoutEditor } from "../hooks/useLayoutEditor";

interface VisualLayoutEditorProps {
  pageName: string;
  visual: Visual;
}

export const VisualLayoutEditor: React.FC<VisualLayoutEditorProps> = ({ pageName, visual }) => {
  const { updateVisualLayout } = useLayoutEditor();

  const getVisualTypeIcon = (type: string) => {
    switch (type) {
      case "card": return "📊";
      case "lineChart": return "📈";
      case "pieChart": return "🍩";
      case "table": return "📋";
      default: return "📦";
    }
  };

  return (
    <div
      data-testid={`visual-${visual.name}`}
      style={{
        position: "absolute",
        left: visual.layout.x,
        top: visual.layout.y,
        width: visual.layout.width,
        height: visual.layout.height,
        backgroundColor: "var(--bg-card)",
        border: "1px solid var(--border-color)",
        borderRadius: "var(--radius-md)",
        boxShadow: "var(--shadow-sm)",
        padding: "var(--space-3)",
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
        transition: "box-shadow 0.15s ease, border-color 0.15s ease",
        boxSizing: "border-box"
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <div style={{ display: "flex", alignItems: "center", gap: "var(--space-1)" }}>
          <span style={{ fontSize: "0.875rem" }}>{getVisualTypeIcon(visual.visualType)}</span>
          <span style={{ fontWeight: 600, fontSize: "0.8125rem", color: "var(--text-primary)" }}>{visual.name}</span>
        </div>
        <span
          style={{
            fontSize: "0.6875rem",
            textTransform: "uppercase",
            letterSpacing: "0.03em",
            padding: "2px 6px",
            borderRadius: "var(--radius-xs)",
            backgroundColor: "var(--bg-subtle)",
            color: "var(--text-secondary)"
          }}
        >
          {visual.visualType}
        </span>
      </div>

      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", paddingTop: "var(--space-2)" }}>
        <span style={{ fontSize: "0.75rem", color: "var(--text-muted)", fontFamily: "monospace" }}>
          x:{visual.layout.x} y:{visual.layout.y}
        </span>
        <button
          onClick={() => updateVisualLayout(pageName, visual.name, { x: visual.layout.x + 10 })}
          aria-label={`move-${visual.name}`}
          className="btn btn-secondary btn-sm"
          style={{ fontSize: "0.75rem", padding: "2px 8px" }}
        >
          Nudge
        </button>
      </div>
    </div>
  );
};
