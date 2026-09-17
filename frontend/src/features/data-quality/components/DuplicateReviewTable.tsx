import React from "react";
import { Badge } from "@shared/ui/Badge/Badge";
import { Button } from "@shared/ui/Button/Button";

export interface DuplicateReviewItem {
  clusterId: string;
  ruleFired: string;
  keptRowId: string;
  droppedRowIds: string[];
  confidenceScore: number;
  reasonCode: string;
}

interface DuplicateReviewTableProps {
  clusters: DuplicateReviewItem[];
  onKeepOverride?: (clusterId: string, newKeptRowId: string) => void;
}

export const DuplicateReviewTable: React.FC<DuplicateReviewTableProps> = ({ clusters, onKeepOverride }) => {
  if (!clusters.length) {
    return (
      <div className="card" style={{ padding: "var(--space-6)", textAlign: "center", color: "var(--text-muted)" }}>
        No duplicate clusters detected. Clean data!
      </div>
    );
  }

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
          <h3 style={{ margin: 0, fontSize: "1.125rem", fontWeight: 600 }}>Duplicate Clusters Review</h3>
          <p style={{ margin: "var(--space-1) 0 0 0", fontSize: "0.75rem", color: "var(--text-muted)" }}>
            Deterministic exact hash, composite key, and similarity clustering matches.
          </p>
        </div>
        <Badge variant="warning">{clusters.length} Duplicate Cluster(s)</Badge>
      </div>

      <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-3)" }}>
        {clusters.map((cluster) => (
          <div
            key={cluster.clusterId}
            style={{
              padding: "var(--space-3) var(--space-4)",
              borderRadius: "var(--radius-md)",
              border: "1px solid var(--border-color)",
              backgroundColor: "var(--bg-subtle)",
              display: "flex",
              flexDirection: "column",
              gap: "var(--space-2)"
            }}
          >
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <span style={{ fontWeight: 600, fontSize: "0.8125rem", color: "var(--text-primary)" }}>
                {cluster.ruleFired}
              </span>
              <Badge variant={cluster.confidenceScore === 1 ? "info" : "warning"}>
                Score: {(cluster.confidenceScore * 100).toFixed(0)}%
              </Badge>
            </div>
            <p style={{ margin: 0, fontSize: "0.75rem", color: "var(--text-secondary)" }}>
              {cluster.reasonCode}
            </p>
            <div style={{ display: "flex", alignItems: "center", gap: "var(--space-2)", flexWrap: "wrap", paddingTop: "var(--space-1)" }}>
              <Badge variant="success">Kept: {cluster.keptRowId}</Badge>
              <Badge variant="danger">Dropped: {cluster.droppedRowIds.join(", ")}</Badge>
              {onKeepOverride && cluster.droppedRowIds.length > 0 && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onKeepOverride(cluster.clusterId, cluster.droppedRowIds[0])}
                  style={{ marginLeft: "auto", fontSize: "0.75rem" }}
                >
                  Swap Kept Row
                </Button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
