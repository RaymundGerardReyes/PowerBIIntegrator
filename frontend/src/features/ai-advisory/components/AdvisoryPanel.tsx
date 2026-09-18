import React from "react";
import { useAdvisoryQuery } from "../hooks/useAdvisoryQuery";
import { ExposureUnlockDialog } from "./ExposureUnlockDialog";
import { Button } from "@shared/ui";

interface AdvisoryPanelProps {
  runId?: string;
  userRole?: string;
  onClose?: () => void;
}

export const AdvisoryPanel: React.FC<AdvisoryPanelProps> = ({
  runId = "run-demo-001",
  userRole = "DataSteward",
  onClose
}) => {
  const {
    isUnlockedConfidential,
    isUnlockModalOpen,
    setIsUnlockModalOpen,
    handleUnlockConfirm
  } = useAdvisoryQuery(runId, userRole);

  const observations = [
    { type: "warning", icon: "⚠", text: "Customer email contains 3.1% null values." },
    { type: "info", icon: "ℹ", text: "transaction_date contains mixed timezone formats." },
    { type: "success", icon: "✓", text: "No severe schema anomalies detected." },
    { type: "info", icon: "ℹ", text: "Column 'category' could be optimized as a categorical dimension." }
  ];

  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", height: "100%", padding: 0 }}>
      {/* Header */}
      <div style={{ padding: "1rem", borderBottom: "1px solid var(--border-color)", display: "flex", alignItems: "center", justifyContent: "space-between", backgroundColor: "var(--bg-subtle)" }}>
        <div>
          <h3 style={{ margin: 0, fontSize: "1rem", fontWeight: 600 }}>AI Advisory</h3>
          <p style={{ margin: "0.25rem 0 0 0", fontSize: "0.75rem", color: "var(--text-secondary)" }}>
            {observations.length} automated observations from run {runId.split("-")[0]}
          </p>
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <button
            type="button"
            onClick={() => setIsUnlockModalOpen(true)}
            className="btn btn-secondary btn-sm"
          >
            <span>{isUnlockedConfidential ? "🔓" : "🔒"}</span> {isUnlockedConfidential ? "Unlocked" : "Unlock Sensitive Data"}
          </button>

          {onClose && (
            <button onClick={onClose} style={{ background: "none", border: "none", cursor: "pointer", fontSize: "1rem", color: "var(--text-muted)" }}>✕</button>
          )}
        </div>
      </div>

      {/* Observations List */}
      <div style={{ padding: "1rem", flex: 1, display: "flex", flexDirection: "column", gap: "0.75rem" }}>
        {observations.map((obs, idx) => (
          <div
            key={idx}
            style={{
              display: "flex",
              alignItems: "flex-start",
              gap: "0.75rem",
              padding: "0.75rem",
              border: "1px solid var(--border-color)",
              borderRadius: "var(--radius-md)",
              backgroundColor: obs.type === "warning" ? "var(--warning-bg)" : obs.type === "success" ? "var(--success-bg)" : "var(--info-bg)"
            }}
          >
            <span
              style={{
                fontSize: "1.25rem",
                lineHeight: 1,
                color: obs.type === "warning" ? "var(--warning)" : obs.type === "success" ? "var(--success)" : "var(--info)"
              }}
            >
              {obs.icon}
            </span>
            <div style={{ flex: 1 }}>
              <p style={{ margin: 0, fontSize: "0.875rem", fontWeight: 500 }}>{obs.text}</p>
              {obs.type === "warning" && (
                <div style={{ marginTop: "0.5rem" }}>
                  <Button variant="secondary" className="btn-sm">Review Records</Button>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Unlock Dialog */}
      <ExposureUnlockDialog
        isOpen={isUnlockModalOpen}
        runId={runId}
        userRole={userRole}
        onClose={() => setIsUnlockModalOpen(false)}
        onConfirmUnlock={handleUnlockConfirm}
      />
    </div>
  );
};

