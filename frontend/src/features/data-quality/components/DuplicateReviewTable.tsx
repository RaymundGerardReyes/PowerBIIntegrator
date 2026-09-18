import React from "react";
import { Badge, Button, EmptyState, DataTable } from "@shared/ui";

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
      <EmptyState
        title="No Duplicate Clusters Detected"
        description="The deterministic hash clustering and similarity models found no duplicates in this dataset."
      />
    );
  }

  const columns = [
    { key: "clusterId" as const, header: "Group ID" },
    { key: "recordsAffected" as const, header: "Records" },
    { key: "ruleFired" as const, header: "Matching Fields" },
    { key: "confidenceScore" as const, header: "Confidence" },
    { key: "action" as const, header: "Decision" }
  ];

  const rows = clusters.map(cluster => ({
    clusterId: cluster.clusterId,
    recordsAffected: 1 + cluster.droppedRowIds.length,
    ruleFired: cluster.ruleFired,
    confidenceScore: `${(cluster.confidenceScore * 100).toFixed(0)}%`,
    action: (
      <div style={{ display: "flex", gap: "0.5rem" }}>
        <Button variant="secondary" className="btn-sm">Review</Button>
        {onKeepOverride && cluster.droppedRowIds.length > 0 && (
          <Button variant="secondary" className="btn-sm" onClick={() => onKeepOverride(cluster.clusterId, cluster.droppedRowIds[0])}>Swap</Button>
        )}
      </div>
    )
  }));

  return (
    <div className="card">
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          borderBottom: "1px solid var(--border-color)",
          paddingBottom: "1rem",
          marginBottom: "1rem"
        }}
      >
        <div>
          <h3 style={{ margin: 0, fontSize: "1rem" }}>Deduplication Review</h3>
          <p style={{ margin: "0.25rem 0 0 0", fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
            Deterministic exact hash, composite key, and similarity clustering matches.
          </p>
        </div>
        <Badge variant="warning">{clusters.length} Duplicate Cluster(s)</Badge>
      </div>

      <DataTable columns={columns} rows={rows} />
    </div>
  );
};
