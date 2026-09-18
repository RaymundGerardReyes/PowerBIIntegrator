import React from "react";
import type { ChartSuggestionDto } from "@shared/types/api-contracts";
import { Badge, Button, EmptyState } from "@shared/ui";

interface ChartSuggestionPanelProps {
  suggestions: ChartSuggestionDto[];
  onSelectSuggestion?: (visualType: string) => void;
}

export const ChartSuggestionPanel: React.FC<ChartSuggestionPanelProps> = ({ suggestions, onSelectSuggestion }) => {
  if (!suggestions.length) {
    return (
      <EmptyState
        title="No Chart Suggestions"
        description="Run the profiling engine to analyze column types and cardinalities to generate visual recommendations."
      />
    );
  }

  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <div style={{ borderBottom: "1px solid var(--border-color)", paddingBottom: "1rem" }}>
        <h3 style={{ margin: 0, fontSize: "1rem" }}>Suggested Visuals</h3>
        <p style={{ margin: "0.25rem 0 0 0", fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
          Deterministic column-role mapping based on data cardinality and type distribution.
        </p>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))", gap: "1rem" }}>
        {suggestions.map((s, idx) => (
          <div key={idx} style={{ padding: "1.25rem", border: "1px solid var(--border-color)", borderRadius: "var(--radius-md)", backgroundColor: "var(--bg-surface)", display: "flex", flexDirection: "column", gap: "1rem" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
              <span style={{ fontWeight: 600, fontSize: "1rem", color: "var(--text-primary)", textTransform: "capitalize" }}>
                {s.recommendedVisualType.replace(/([A-Z])/g, ' $1').trim()}
              </span>
              <Badge variant="info">{(s.confidenceScore * 100).toFixed(0)}% Confidence</Badge>
            </div>
            
            <div>
              <span style={{ fontSize: "0.75rem", fontWeight: 700, color: "var(--text-muted)", textTransform: "uppercase", display: "block", marginBottom: "0.25rem" }}>REASON</span>
              <p style={{ margin: 0, fontSize: "0.875rem", color: "var(--text-secondary)", lineHeight: 1.5 }}>
                {s.reason}
              </p>
            </div>

            <div style={{ marginTop: "auto", paddingTop: "1rem", borderTop: "1px solid var(--border-color)" }}>
              {onSelectSuggestion && (
                <Button variant="secondary" className="btn-sm" onClick={() => onSelectSuggestion(s.recommendedVisualType)}>
                  Add to Report
                </Button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
