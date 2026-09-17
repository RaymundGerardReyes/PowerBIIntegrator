import React from "react";
import type { DatasetProfileDto } from "@shared/types/api-contracts";
import { Badge } from "@shared/ui/Badge/Badge";

interface ProfileSummaryPanelProps {
  profile: DatasetProfileDto | null;
  isLoading?: boolean;
}

export const ProfileSummaryPanel: React.FC<ProfileSummaryPanelProps> = ({ profile, isLoading }) => {
  if (isLoading) {
    return (
      <div className="card" style={{ padding: "var(--space-6)", textAlign: "center", color: "var(--text-muted)" }}>
        Profiling dataset deterministically...
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="card" style={{ padding: "var(--space-6)", textAlign: "center", color: "var(--text-muted)" }}>
        No dataset profile available. Please select a source.
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
          <h3 style={{ margin: 0, fontSize: "1.125rem", fontWeight: 600 }}>{profile.datasetName}</h3>
          <p style={{ margin: "var(--space-1) 0 0 0", fontSize: "0.75rem", color: "var(--text-muted)" }}>
            Source: <code style={{ color: "var(--text-secondary)" }}>{profile.sourceReference}</code> | Total Rows: {profile.totalRows.toLocaleString()}
          </p>
        </div>
        <Badge variant="info">Deterministic Profile</Badge>
      </div>

      <div
        style={{
          overflowX: "auto",
          borderRadius: "var(--radius-md)",
          border: "1px solid var(--border-color)",
          backgroundColor: "var(--bg-surface)"
        }}
      >
        <table>
          <thead>
            <tr>
              <th>Column</th>
              <th>Inferred Type</th>
              <th>Null %</th>
              <th>Distinct</th>
              <th>Cardinality</th>
              <th>Regex Signature</th>
            </tr>
          </thead>
          <tbody>
            {profile.columnProfiles.map((col) => (
              <tr key={col.columnName}>
                <td style={{ fontWeight: 600, color: "var(--text-primary)" }}>{col.columnName}</td>
                <td style={{ fontFamily: "monospace", fontSize: "0.75rem", color: "var(--text-secondary)" }}>{col.inferredType}</td>
                <td>{(col.nullRatio * 100).toFixed(1)}%</td>
                <td>{col.distinctCount.toLocaleString()}</td>
                <td>
                  <Badge
                    variant={
                      col.cardinalityClass === "High" ? "danger" :
                      col.cardinalityClass === "Medium" ? "warning" : "success"
                    }
                  >
                    {col.cardinalityClass}
                  </Badge>
                </td>
                <td style={{ fontFamily: "monospace", fontSize: "0.75rem", color: "var(--text-muted)", maxWidth: "240px", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                  {col.detectedPatternRegex}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
