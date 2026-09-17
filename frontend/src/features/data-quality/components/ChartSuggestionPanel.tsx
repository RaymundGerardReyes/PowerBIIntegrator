import React from "react";
import type { ChartSuggestionDto } from "@shared/types/api-contracts";
import { Badge } from "@shared/ui/Badge/Badge";
import { Button } from "@shared/ui/Button/Button";

interface ChartSuggestionPanelProps {
  suggestions: ChartSuggestionDto[];
  onSelectSuggestion?: (visualType: string) => void;
}

export const ChartSuggestionPanel: React.FC<ChartSuggestionPanelProps> = ({ suggestions, onSelectSuggestion }) => {
  if (!suggestions.length) {
    return (
      <div className="card" style={{ padding: "var(--space-6)", textAlign: "center", color: "var(--text-muted)" }}>
        No chart suggestions available.
      </div>
    );
  }

  const getVisualIcon = (type: string) => {
    switch (type) {
      case "lineChart": return "📈";
      case "barChart": return "📊";
      case "scatterPlot": return "🎯";
      default: return "📋";
    }
  };

  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: "var(--space-4)" }}>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          borderBottom: "1px solid var(--border-color)",
          paddingBottom: "var(--space-3)"
        }}
      >
        <div>
          <h3 style={{ margin: 0, fontSize: "1.125rem", fontWeight: 600 }}>Recommended Visuals</h3>
          <p style={{ margin: "var(--space-1) 0 0 0", fontSize: "0.75rem", color: "var(--text-muted)" }}>
            Heuristic column-role mapping (Deterministic, no ML).
          </p>
        </div>
        <Badge variant="success">PBIR Target Ready</Badge>
      </div>

      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))",
          gap: "var(--space-3)"
        }}
      >
        {suggestions.map((s, idx) => (
          <div
            key={idx}
            style={{
              padding: "var(--space-4)",
              borderRadius: "var(--radius-md)",
              border: "1px solid var(--border-color)",
              backgroundColor: "var(--bg-subtle)",
              display: "flex",
              flexDirection: "column",
              justifyContent: "space-between",
              gap: "var(--space-3)"
            }}
          >
            <div>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "var(--space-1)" }}>
                <div style={{ display: "flex", alignItems: "center", gap: "var(--space-2)" }}>
                  <span style={{ fontSize: "1.125rem" }}>{getVisualIcon(s.recommendedVisualType)}</span>
                  <span style={{ fontWeight: 600, fontSize: "0.9375rem", color: "var(--text-primary)", textTransform: "capitalize" }}>
                    {s.recommendedVisualType}
                  </span>
                </div>
                <Badge variant="info">{(s.confidenceScore * 100).toFixed(0)}% Match</Badge>
              </div>
              <p style={{ margin: 0, fontSize: "0.8125rem", color: "var(--text-secondary)", lineHeight: 1.45 }}>
                {s.reason}
              </p>
            </div>

            {onSelectSuggestion && (
              <Button
                variant="secondary"
                size="sm"
                onClick={() => onSelectSuggestion(s.recommendedVisualType)}
                style={{ width: "100%" }}
              >
                Add to Dashboard Canvas
              </Button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};
